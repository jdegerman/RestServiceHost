using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RestServiceHost
{
    public abstract class RestServiceBase
    {
        public HttpListenerRequest Request { get; set; }
        public HttpListenerResponse Response { get; set; }
    }
}
