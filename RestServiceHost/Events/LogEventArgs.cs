using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestServiceHost.Events
{
    public class LogEventArgs
    {
        public EventLogEntryType EntryType { get; set; }
        public string Message { get; set; }
        public LogEventArgs(EventLogEntryType entryType, string message, params object[] args)
        {
            EntryType = entryType;
            Message = string.Format(message, args);
        }
    }
}
