using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace XMovie.Service
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmDialog(string title, string message);

        void ShowSettingWindow();

        string ShowFolderDialog(string title, string basePath);
    }
}
