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

        public void Debug(string msg)
        {
            FireLogEvent(msg, LogLevel.Debug);
        }

        public void Information(string msg)
        {
            FireLogEvent(msg, LogLevel.Information);
        }

        public void Warning(string msg)
        {
            FireLogEvent(msg, LogLevel.Warning);
        }

        public void Error(string msg)
        {
            FireLogEvent(msg, LogLevel.Error);
        }

        private void FireLogEvent(string msg, LogLevel level)
        {
            LogEvent?.Invoke(new LogRecord(msg, level));
        }
    }
}
