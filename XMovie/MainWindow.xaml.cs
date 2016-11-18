using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XMovie.Common;
using MahApps.Metro.Controls;

namespace XMovie
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private Logger logger;
        private ObservableCollection<LogRecord> logRecords;

        public MainWindow()
        {
            InitializeComponent();

            logRecords = new ObservableCollection<LogRecord>();
            LogListView.DataContext = logRecords;
        }

        private void XMovieWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.LogEvent += Logger_LogEvent;
            logger = Logger.Instace;
        }

        private void Logger_LogEvent(LogRecord record)
        {
            // FIXME: アプリ終了時のタイミングで落ちるかも
            Dispatcher.Invoke(() =>
            {
                logRecords.Add(record);
                LogListView.ScrollIntoView(record);
            });
        }
    }
}
