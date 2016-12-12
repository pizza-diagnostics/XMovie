using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

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

        public string ShowFolderDialog(string title, string basePath)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = title;
            dlg.InitialDirectory = basePath;

            dlg.IsFolderPicker = true;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = basePath;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dlg.FileName;
            }
            else
            {
                return null;
            }
        }
    }
}
