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

        public static event LogDelegate LogEvent;

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

        public void Debug(string fmt, params object[] args)
        {
            FireLogEvent(String.Format(fmt, args), LogLevel.Debug);
        }

        public void Debug(object arg)
        {
            FireLogEvent(arg?.ToString(), LogLevel.Debug);
        }

        public void Information(string fmt, params object[] args)
        {
            FireLogEvent(String.Format(fmt, args), LogLevel.Information);
        }

        public void Information(object arg)
        {
            FireLogEvent(arg?.ToString(), LogLevel.Information);
        }

        public void Warning(string fmt, params object[] args)
        {
            FireLogEvent(String.Format(fmt, args), LogLevel.Warning);
        }

        public void Warning(object arg)
        {
            FireLogEvent(arg?.ToString(), LogLevel.Warning);
        }

        public void Error(string fmt, params object[] args)
        {
            FireLogEvent(String.Format(fmt, args), LogLevel.Error);
        }

        public void Error(object arg)
        {
            FireLogEvent(arg?.ToString(), LogLevel.Error);
        }

        private void FireLogEvent(string message, LogLevel level)
        {
            LogEvent?.Invoke(new LogRecord(message, level));
        }
    }
}
