using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Common
{
    public delegate void LogDelegate(LogRecord record);

    public sealed class Logger
    {
        public event LogDelegate LogEvent;

        public Logger() { }

        public void Debug(object msg)
        {
            FireLogEvent(msg, LogLevel.Debug);
        }

        public void Information(object msg)
        {
            FireLogEvent(msg, LogLevel.Information);
        }

        public void Warning(object msg)
        {
            FireLogEvent(msg, LogLevel.Warning);
        }

        public void Error(object msg)
        {
            FireLogEvent(msg, LogLevel.Error);
        }

        private void FireLogEvent(object msg, LogLevel level)
        {
            LogEvent?.Invoke(new LogRecord(msg.ToString(), level));
        }
    }
}
