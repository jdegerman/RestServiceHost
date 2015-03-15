using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestServiceHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = RestServiceHost.Configuration.ServiceConfig.Load("Configuration\\service.xml");
            var api = new WebAPI("http://localhost:8080/");
            api.Start();
            while (true)
                System.Threading.Thread.Sleep(1000);
        }
    }
}
