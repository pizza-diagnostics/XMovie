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
        private static readonly object syncObj = new Object();
        private static Logger instance = null;

        public event LogDelegate LogEvent;

        private Logger() { }

        public static Logger Instace
        {
            get
            {
                if (instance == null)
                {
                    lock (syncObj)
                    {
                        if (instance == null)
                        {
                            instance = new Logger();
                        }
                    }
                }
                return instance;
            }
        }

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
