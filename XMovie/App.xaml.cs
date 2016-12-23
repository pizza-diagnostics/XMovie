using System;
using System.Threading;
using System.Windows;

namespace XMovie
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private Mutex mutex;

        private EventWaitHandle waitHandle;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool isOwned;
            mutex = new Mutex(true, "XMovie-91bdd365-4e94-4378-86c4-ebbafef145e2", out isOwned);
            waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "XMovie-410b216e-7aba-4d09-b5c8-ffc861802df4");

            if (isOwned)
            {
                var thread = new Thread(() =>
                {
                    while (waitHandle.WaitOne())
                    {
                        Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            var window = ((MainWindow)Current.MainWindow);
                            if (window.WindowState == WindowState.Minimized || window.Visibility == Visibility.Hidden)
                            {
                                window.Show();
                                window.WindowState = WindowState.Normal;
                            }

                            window.Activate();
                            window.Topmost = true;
                            window.Topmost = false;
                            window.Focus();
                        }));
                    }
                });

                thread.IsBackground = true;
                thread.Start();
            }
            else
            {
                waitHandle.Set();
                Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
            }
        }
    }
}
