using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using XMovie.Common;

namespace XMovie.ViewModels
{
    public class LogListViewModel : ViewModelBase
    {
        public ObservableCollection<LogRecord> LogRecords { get; private set; } = new ObservableCollection<LogRecord>();

        public void LogViewLoaded()
        {
            Logger.Instace.LogEvent += Logger_LogEvent;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.ShutdownStarted += (s, e) =>
            {
                Logger.Instace.LogEvent -= Logger_LogEvent;
            };
        }

        private void Logger_LogEvent(LogRecord record)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LogRecords.Add(record);
                OnPropertyChanged("LastLogSummary");
            });
        }

        public string LastLogSummary
        {
            get { return LogRecords.LastOrDefault()?.MessageSummary; }
        }
    }
}
