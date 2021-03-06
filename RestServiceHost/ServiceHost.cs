﻿using RestServiceHost.Configuration;
using RestServiceHost.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RestServiceHost
{
    public class ServiceHost
    {
        private List<WebAPI> services = new List<WebAPI>();
        private Dictionary<string, System.Reflection.Assembly> assemblies = new Dictionary<string, System.Reflection.Assembly>();

        public delegate void OnLogHandler(object sender, LogEventArgs e);
        public event OnLogHandler OnLogEntry;

        public ServiceHost(ServiceConfig configuration, OnLogHandler onLogCallback = null)
        {
            if (onLogCallback != null)
                OnLogEntry += onLogCallback;
            LoadAssemblies(configuration);
            CreateServices(configuration);
        }

        private void CreateServices(ServiceConfig configuration)
        {
            foreach (var service in configuration.Services)
            {
                Info("Creating service '{0}'", service.Name);
                service.Urls.ForEach(url => Info(" * Binding to URL '{0}'", url));
                var webService = new WebAPI(service.Name, service.Urls.ToArray());
                webService.OnLogEntry += webService_OnLogEntry;
                RegisterControllers(service, webService);
            }
        }

        private void RegisterControllers(Service service, WebAPI webService)
        {
            foreach (var controller in service.Controllers)
            {
                Info(" * Registering controller '{0}'", controller.Assembly);
                ThrowIf(!assemblies.ContainsKey(controller.Assembly), "Assembly '{0}' not defined", controller.Assembly);
                var assembly = assemblies[controller.Assembly];
                var controllerInstance = (RestServiceBase)assembly.CreateInstance(controller.FullyQualifiedName);
                var controllerInstanceType = controllerInstance.GetType();
                foreach(var method in controllerInstanceType.GetMethods())
                {
                    // Skip property accessor methods, and default object methods
                    if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                        continue;
                    if (typeof(object).GetMethods().Any(objectMethod => objectMethod.Name == method.Name))
                        continue;
                    Info("  * Found method '{0}' with return type {1}", method.Name, method.ReturnParameter.ToString());
                    foreach(var param in method.GetParameters())
                    {
                        Info("   * Parameter '{0}' with type {1}", param.Name, param.ParameterType.Name);
                    }
                }
                webService.RegisterController(controller.Assembly, controllerInstance);
                services.Add(webService);
            }
        }

        private void LoadAssemblies(ServiceConfig configuration)
        {
            foreach (var assembly in configuration.Assemblies)
            {
                Info("Loading assembly '{0}' from '{1}'", assembly.Name, assembly.Path);
                ThrowIf(assemblies.ContainsKey(assembly.Name), "Assembly '{0}' already defined");
                var fullPath = Path.GetFullPath(assembly.Path);
                assemblies.Add(assembly.Name, System.Reflection.Assembly.LoadFrom(fullPath));
            }
        }

        public void Start(string name = null)
        {
            InvokeServiceMethod("Start", name);
        }

        public void Stop(string name = null)
        {
            InvokeServiceMethod("Stop", name);
        }

        private void InvokeServiceMethod(string methodName, string serviceName)
        {
            var method = typeof(WebAPI).GetMethod(methodName);
            if (string.IsNullOrEmpty(serviceName))
            {
                services.ForEach(service => method.Invoke(service, null));
            }
            else
            {
                ThrowIf(!services.Any(service => service.Name == methodName), "Service '{0}' does not exist", methodName);
                method.Invoke(services.First(service => service.Name == methodName), null);
            }
        }

        private void ThrowIf(bool cond, string text, params object[] args)
        {
            if (!cond)
                return;
            Error(text, args);
            throw new Exception(string.Format(text, args));
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

        void webService_OnLogEntry(object sender, LogEventArgs e)
        {
            var handler = OnLogEntry;
            if (handler == null)
                return;
            handler(sender, e); // Forward log events to subscriber
        }
        private void Log(EventLogEntryType type, string message, params object[] args)
        {
            var handler = OnLogEntry;
            if (handler == null)
                return;
            handler(this, new LogEventArgs(type, message, args));
        }
        #endregion
    }
}
