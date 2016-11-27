using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Common
{
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
    }

    public class LogRecord
    {
        public LogRecord(string message, LogLevel level)
        {
            Time = DateTime.Now;
            Level = level;
            Message = message;
        }

        public LogLevel Level { get; private set; }
        public DateTime Time { get; private set; }
        public string Message { get; private set; }

        public string MessageSummary
        {
            get
            {
                var lines = Message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Length > 0 ? lines.First() : "";
            }
        }
    }
}
