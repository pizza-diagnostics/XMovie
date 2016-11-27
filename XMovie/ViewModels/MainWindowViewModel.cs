using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models;
using XMovie.Models.Settings;

namespace XMovie.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private UserSettings userSettings;

        public MainWindowViewModel()
        {
            userSettings = UserSettingManager.Instance.GetUserSettings();
        }

        public void MainWindowLoaded()
        {
        }

        public int ThumbnailCount
        {
            get { return userSettings.ThumbnailCount; }
            set
            {
                if (value < 1 || value > 5)
                {
                    SetError("thumbnailCount", "サムネイル数は1から5に設定する必要があります。");
                }
                else
                {
                    userSettings.ThumbnailCount = value;
                }
            }
        }

        public List<string> ThumbnailCountList
        {
            get
            {
                return new List<string>(new string[]
                {
                    "Thumbnail 1x1",
                    "Thumbnail 2x1",
                    "Thumbnail 3x1",
                    "Thumbnail 4x1",
                    "Thumbnail 5x1",
                });
            }
        }
        public int ThumbnailCountListIndex
        {
            get { return userSettings.ThumbnailCount - 1; }
            set
            {
                userSettings.ThumbnailCount = value + 1;
            }
        }

        public int MainWindowWidth
        {
            get { return userSettings.MainWindowWidth; }
            set { userSettings.MainWindowWidth = value; }
        }

        public int MainWindowHeight
        {
            get { return userSettings.MainWindowHeight; }
            set { userSettings.MainWindowHeight = value; }
        }

        public int MainWindowTop
        {
            get { return userSettings.MainWindowTop; }
            set { userSettings.MainWindowTop = value; }
        }

        public int MainWindowLeft
        {
            get { return userSettings.MainWindowLeft; }
            set { userSettings.MainWindowLeft = value; }
        }

        public WindowState MainWindowState
        {
            get { return userSettings.MainWindowState; }
            set { userSettings.MainWindowState = value; }
        }

        #region Command
        private RelayCommand windowClosingCommand;
        public ICommand WindowClosingCommand
        {
            get
            {
                if (windowClosingCommand == null)
                {
                    windowClosingCommand = new RelayCommand((param) =>
                    {
                        UserSettingManager.Instance.Save();
                    });
                }
                return windowClosingCommand;

            }
        }
        #endregion
    }
}
