using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
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

        [IgnoreDataMember]
        public GridLength InfoColumnWidth
        {
            get { return new GridLength(MainWindowInfoColumnWidth, GridUnitType.Pixel); }
            set { MainWindowInfoColumnWidth = (int)value.Value; }
        }
        public int MainWindowInfoColumnWidth { get; set; } = 100;

        [IgnoreDataMember]
        public GridLength LogRowHeight
        {
            get { return new GridLength(MainWindowLogRowHeigh); }
            set { MainWindowLogRowHeigh = (int)value.Value; }
        }
        public int MainWindowLogRowHeigh { get; set; } = 100;

        public int SorterIndex { get; set; } = 0;

        public bool IsFileSearch { get; set; } = false;
        public ObservableCollection<string> SearchHistories { get; set; }
            = new ObservableCollection<string>();

        public ObservableCollection<DirectoryMonitorSettings> DirectoryMonitors { get; set; } 
            = new ObservableCollection<DirectoryMonitorSettings>();

        public ObservableCollection<FileExtensionSettings> CustomMovieExtensions { get; set; }
            = new ObservableCollection<FileExtensionSettings>();

        public ObservableCollection<FileExtensionSettings> DefaultMovieExtensions { get; set; }
            = FileExtensionSettings.GetDefaultMovieExtensions();

        public bool IsEnableDebugLog { get; set; } = true;
        public bool IsEnableInfoLog { get; set; } = true;
        public bool IsEnableWarningLog { get; set; } = true;
        public bool IsEnableErrorLog { get; set; } = true;

        public UserSettings()
        {
            thumbnailCount = 3;
            CheckUpdateDefaultMovieExtensions();
        }

        public List<string> GetImportableMovieExtensions()
        {
            var importable = new List<string>();

            var exts = new ICollection<FileExtensionSettings>[]
            {
                DefaultMovieExtensions,
                CustomMovieExtensions
            };

            foreach (var list in exts)
            {
                var l = list.Where(e => e.IsEnabled).Select(e => "." + e.Ext.ToLower()).ToList();
                if (l.Count() > 0)
                {
                    importable.AddRange(l);
                }
            }
            return importable;
        }

        private void CheckUpdateDefaultMovieExtensions()
        {
            var defaultSettings = FileExtensionSettings.GetDefaultMovieExtensions();
            var list = DefaultMovieExtensions.ToList();
            var lost = new List<FileExtensionSettings>();
            foreach (var s in defaultSettings)
            {
                if (list.Find(t => s.Ext.Equals(t.Ext)) == null)
                {
                    lost.Add(s);
                }
            }
            foreach (var s in lost)
            {
                DefaultMovieExtensions.Add(s);
            }
        }
    }
}
