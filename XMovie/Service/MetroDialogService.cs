using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.Windows;

namespace XMovie.Service
{
    public class MetroDialogService : IDialogService
    {
        public async Task<bool> ShowConfirmDialog(string title, string message)
        {
            var metroWindow = (Application.Current.MainWindow as MetroWindow);
            var setting = new MetroDialogSettings();
            setting.AnimateShow = false;
            setting.AnimateHide = false;
            setting.DefaultButtonFocus = MessageDialogResult.Negative;

            var dialogResult = await metroWindow.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, setting);

            return dialogResult == MessageDialogResult.Affirmative;
        }

        public void ShowSettingWindow()
        {
            var window = new SettingWindow();
            window.ShowDialog();
        }
    }
}
