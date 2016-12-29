using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models.Settings;

namespace XMovie.ViewModels
{
    public class LogListViewModel : BindableBase
    {
        public ObservableCollection<LogRecord> LogRecords { get; private set; } = new ObservableCollection<LogRecord>();

        private UserSettings settings = UserSettingManager.Instance.GetUserSettings();

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
            var tbl = new Dictionary<LogLevel, bool>()
            {
                { LogLevel.Debug, IsEnableDebugLog },
                { LogLevel.Information, IsEnableInfoLog },
                { LogLevel.Warning, IsEnableWarningLog },
                { LogLevel.Error, IsEnableErrorLog },
            };
            if (tbl[record.Level])
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LogRecords.Add(record);
                    OnPropertyChanged("LastLogSummary");
                });
            }
        }

        public string LastLogSummary
        {
            get { return LogRecords.LastOrDefault()?.MessageSummary; }
        }

        public bool IsEnableDebugLog
        {
            get { return settings.IsEnableDebugLog; }
            set { settings.IsEnableDebugLog = value; }
        }

        public bool IsEnableInfoLog
        {
            get { return settings.IsEnableInfoLog; }
            set { settings.IsEnableInfoLog = value; }
        }

        public bool IsEnableWarningLog
        {
            get { return settings.IsEnableWarningLog; }
            set { settings.IsEnableWarningLog = value; }
        }

        public bool IsEnableErrorLog
        {
            get { return settings.IsEnableErrorLog; }
            set { settings.IsEnableErrorLog = value; }
        } 

        // NOTE: 現状filterではなく追加自体を行わないようにしている
        /*
        private void UpdateLogFilter()
        {
            var collectionView = CollectionViewSource.GetDefaultView(LogRecords);
            collectionView.Filter = (record) =>
            {
                var tbl = new Dictionary<LogLevel, bool>()
                {
                    { LogLevel.Debug, IsEnableDebugLog },
                    { LogLevel.Information, IsEnableInfoLog },
                    { LogLevel.Warning, IsEnableWarningLog },
                    { LogLevel.Error, IsEnableErrorLog },
                };
                return tbl[((LogRecord)record).Level];
            };
            collectionView.Refresh();
        }
        */

        private ICommand clearLogCommand;
        public ICommand ClearLogCommand
        {
            get
            {
                return clearLogCommand ?? (clearLogCommand = new RelayCommand((param) => { LogRecords.Clear(); }));
            }
        }

        private ICommand setLogLevelCommand;
        public ICommand SetLogLevelCommand
        {
            get
            {
                return setLogLevelCommand ?? (setLogLevelCommand = new RelayCommand((param) =>
                {

                }));
            }
        }
             
    }
}
