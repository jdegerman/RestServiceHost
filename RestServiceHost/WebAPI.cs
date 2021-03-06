﻿using RestServiceHost.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;

namespace RestServiceHost
{
    public class WebAPI
    {
        private HttpListener listener;
        private Dictionary<string, RestServiceBase> controllers = new Dictionary<string, RestServiceBase>();

        public bool IsListening { get { return listener.IsListening; } }
        public string Name { get; private set; }
        public delegate void OnLogHandler(object sender, LogEventArgs e);
        public event OnLogHandler OnLogEntry;

        public WebAPI(string name, params string[] urls)
        {
            Name = name;
            listener = new HttpListener();
            foreach (var url in urls)
            {
                listener.Prefixes.Add(url);
            }
        }
        public void RegisterUrl(string url)
        {
            listener.Prefixes.Add(url);
        }
        public void Start()
        {
            Info("[{0}] Starting service", Name);
            listener.Start();
            ThreadPool.QueueUserWorkItem(ListenAsync);
        }
        public void Stop()
        {
            Info("[{0}] Stopping service", Name);
            listener.Stop();
        }
        public void RegisterController(string name, RestServiceBase controller)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Default";
            ThrowIf(controllers.ContainsKey(name), "Controller '{0}' already registered", name);
            controllers.Add(name, controller);
        }
        public void UnregisterController(string name)
        {
            ThrowIf(!controllers.ContainsKey(name), "Controller '{0}' not registered", name);
            controllers.Remove(name);
        }

        private async void ListenAsync(object n)
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                await Task.Factory.StartNew(() => HandleRequest(context));
            }
            Info("[{0}] Service has stopped listening", Name);
        }

        private async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var route = ExtractRoutingInformation(request);
            if (route.Controller == "Default" && route.Method == "favicon.ico")
                goto Exit;
            Task temp = null;
            response.ContentType = "application/json";
            response.StatusCode = 200;
            if (!controllers.ContainsKey(route.Controller))
            {
                Warn("[{0}] Controller '{1}' does not exist", Name, route.Controller);
                await HandleError(response, "Controller '{0}' does not exist", route.Controller);
                goto Exit;
            }
            var controller = controllers[route.Controller];
            controller.Request = request;
            controller.Response = response;
            var method = controller.GetMethodByName(route.Method);
            if (method == null)
            {
                Warn("[{0}] Controller '{1}' does not support method '{2}'", Name, route.Controller, route.Method);
                await HandleError(response, "Controller '{0}' does not support method '{1}'", route.Controller, route.Method);
                goto Exit;
            }
            try
            {
                var resolvedParametersArray = ResolveMethodParameters(request, method);
                await SendResponse(response, true, method.Invoke(controller, resolvedParametersArray));
            }
            catch (Exception ex)
            {
                temp = HandleError(response, "An error occurred: " + ex.Message); // Because we can't await in a catch
            }
            if (temp != null)
                await temp;
        Exit:
            response.OutputStream.Close();
            response.Close();
        }

        private static object[] ResolveMethodParameters(HttpListenerRequest request, System.Reflection.MethodInfo method)
        {
            object[] resolvedParametersArray = null;
            var methodParameters = method.GetParameters();
            if (methodParameters.Length > 0)
            {
                var parameters = ExtractParameters(request);
                var resolvedParameters = new List<object>();
                foreach (var methodParameter in methodParameters)
                {
                    if (parameters.ContainsKey(methodParameter.Name))
                    {
                        resolvedParameters.Add(ConvertFromString(parameters[methodParameter.Name], methodParameter.ParameterType));
                    }
                    else
                    {
                        if (!methodParameter.IsOptional && methodParameter.DefaultValue != null)
                            resolvedParameters.Add(Activator.CreateInstance(methodParameter.ParameterType));
                        else
                            resolvedParameters.Add(null); // Just add null and hope for the best
                    }
                }
                resolvedParametersArray = resolvedParameters.ToArray();
            }
            return resolvedParametersArray;
        }

        private async Task HandleError(HttpListenerResponse response, string error, params object[] args)
        {
            await SendResponse(response, false, string.Format(error, args));
        }

        private async Task SendResponse(HttpListenerResponse response, bool success, object responseData)
        {
            try
            {
                if (response.OutputStream.CanWrite)
                {
                    var responseObject = new { success, responseData };
                    var errorMessage = Encoding.Default.GetBytes(Serialize(responseObject));
                    await response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length);
                }
            }
            catch(Exception ex)
            {
                Warn("[{0}] Unable to send response to client: ", Name, ex.Message);
            }
        }

        private string Serialize(object responseObject)
        {
            return Json.Encode(responseObject);
        }

        private Routing ExtractRoutingInformation(HttpListenerRequest request)
        {
            var r = new Routing { Controller = "Default", Method = "Index" };
            var requestUrlParts = request.Url.LocalPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (requestUrlParts.Length == 1)
            {
                r.Method = requestUrlParts[0];
            }
            else if (requestUrlParts.Length > 1)
            {
                r.Controller = requestUrlParts[0];
                r.Method = requestUrlParts[1];
            }
            if (r.Method != "favicon.ico")
                Info("[{0}] Invoking method '{1}'", Name, r.Method);
            return r;
        }
        private static Dictionary<string, string> ExtractParameters(HttpListenerRequest request)
        {
            var requestParameters = new Dictionary<string, string>();
            foreach (var key in request.QueryString.AllKeys)
            {
                var value = request.QueryString[key];
                requestParameters.Add(key, value);
            }
            if (request.HttpMethod == "POST" && request.ContentLength64 > 0)
            {
                using (var reader = new StreamReader(request.InputStream))
                {
                    var postData = reader.ReadToEnd();
                    var postCollection = HttpUtility.ParseQueryString(HttpUtility.UrlDecode(postData));
                    foreach (var key in postCollection.AllKeys)
                    {
                        var value = postCollection[key];
                        requestParameters.Add(key, value);
                    }
                }
            }
            return requestParameters;
        }
        private void ThrowIf(bool cond, string text, params object[] args)
        {
            if (cond)
                throw new Exception(string.Format(text, args));
        }
        private static object ConvertFromString(string value, Type type)
        {
            if (value == null)
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
            if (type.IsArray)
            {
                var arrayType = type.GetElementType();
                var listBaseType = typeof(List<>);
                var listType = listBaseType.MakeGenericType(arrayType);
                var list = Activator.CreateInstance(listType);
                foreach(var item in value.Split(','))
                {
                    var add = list.GetMethodByName("Add");
                    add.Invoke(list, new object[1] { ConvertFromString(item, arrayType) });
                }
                var toArray = list.GetMethodByName("ToArray");
                return toArray.Invoke(list, null);
            }
            else if (type == typeof(Guid))
            {
                return Guid.Parse(value);
            }
            else if (type == typeof(bool))
            {
                return value != null && (value.ToLower() == "on" || bool.Parse(value));
            }
            else
            {
                return Convert.ChangeType(value, type);
            }
        }
        #region - Logging -
        private void Info(string message, params object[] args)
        {
            Log(EventLogEntryType.Information, message, args);
        }
        private void Warn(string message, params object[] args)
        {
            Log(EventLogEntryType.Warning, message, args);
        }
        private void Error(string message, params object[] args)
        {
            Log(EventLogEntryType.Error, message, args);
        }
        private void Log(EventLogEntryType type, string message, params object[] args)
        {
            var handler = OnLogEntry;
            if (handler == null)
                return;
            handler(this, new LogEventArgs(type, message, args));
        }
        #endregion
        private class Routing
        {
            public string Controller { get; set; }
            public string Method { get; set; }
        }
    }
}
