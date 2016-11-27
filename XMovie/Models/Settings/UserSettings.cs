using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XMovie.Models.Settings
{
    public class UserSettings
    {
        public event EventHandler<int> ThumbnailCountChanged;

        private int thumbnailCount;
        public int ThumbnailCount
        {
            get { return thumbnailCount; }
            set
            {
                if (thumbnailCount != value)
                {
                    thumbnailCount = value;
                    ThumbnailCountChanged?.Invoke(this, value);
                }
            }
        }

        public int MainWindowWidth { get; set; } = 800;
        public int MainWindowHeight { get; set; } = 600;
        public int MainWindowTop { get; set; } = 100;
        public int MainWindowLeft { get; set; } = 100;
        public WindowState MainWindowState { get; set; } = WindowState.Normal;

        public bool IsFileSearch { get; set; } = false;

        public UserSettings()
        {
            thumbnailCount = 3;
        }
    }
}
