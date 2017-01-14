using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;
using XMovie.Models;

namespace XMovie.Common.Behaviors
{
    public class WindowClosingBehavior : Behavior<Window>
    {
        private bool IsCleanedUp { get; set; } = false;

        private void CleanUp()
        {
            if (!IsCleanedUp)
            {
                IsCleanedUp = true;
                AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
                AssociatedObject.Closing -= AssociatedObject_Closing;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            AssociatedObject.Closing += AssociatedObject_Closing;
        }

        private async void AssociatedObject_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            var logger = App.Container.Resolve<Logger>();
            logger.Information("終了処理");

            // MD5
            logger.Information("MD5算出処理終了待機中...");
            var calc = App.Container.Resolve<MD5Calculator>();
            await calc.Shutdown();
            logger.Information("完了");

            // Import
            logger.Information("動画登録処理終了待機中...");
            var movieDispatcher = App.Container.Resolve<MovieDispatcher>();
            await movieDispatcher.Shutdown();
            logger.Information("完了");

            // TODO: MainWindowViewModel Shutdown

            App.Current.Shutdown();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            CleanUp();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUp();
        }
    }
}
