using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using XMovie.Common;

namespace XMovie.ViewModel
{
    public class LogListViewModel : ViewModelBase
    {
        public ObservableCollection<LogRecord> LogRecords { get; private set; } = new ObservableCollection<LogRecord>();

        public event EventHandler LogRecordAdded;

        public void LogViewLoaded()
        {
            Logger.Instace.LogEvent += Logger_LogEvent;
        }

        private void Logger_LogEvent(LogRecord record)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LogRecords.Add(record);
            });
        }
    }
}
