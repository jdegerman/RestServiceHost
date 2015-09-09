using RestServiceHost.Events;
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
            var host = new ServiceHost(config, host_OnLogEntry);
            host.Start();
            Console.WriteLine("Press any key to shut down service...");
            Console.Read();
            host.Stop();
        }

        static void host_OnLogEntry(object sender, LogEventArgs e)
        {
            ConsoleColor color;
            switch(e.EntryType)
            {
                case System.Diagnostics.EventLogEntryType.Error:
                    color = ConsoleColor.Red;
                    break;
                case System.Diagnostics.EventLogEntryType.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                default:
                    color = ConsoleColor.White;
                    break;
            }
            Console.ForegroundColor = color;
            Console.WriteLine(e.Message);
        }
    }
}
