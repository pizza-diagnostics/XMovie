using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models.Settings;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class ToolViewModel : BindableBase
    {
        private IDialogService dialogService;
        private UserSettings userSettings = UserSettingManager.Instance.GetUserSettings();

        public ToolViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            LoadFromSettings();
        }

        public void Commit()
        {
            userSettings.DefaultMovieExtensions = DefaultMovieExtensions;
            userSettings.CustomMovieExtensions = CustomMovieExtensions;
            userSettings.DirectoryMonitors = DirectoryMonitors;
        }

        public void Rollback()
        {
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            DirectoryMonitors =
                new ObservableCollection<DirectoryMonitorSettings>(userSettings.DirectoryMonitors.Select(m => (DirectoryMonitorSettings)m.Clone()));
            DefaultMovieExtensions =
                new ObservableCollection<FileExtensionSettings>(userSettings.DefaultMovieExtensions.Select(e => (FileExtensionSettings)e.Clone()));
            CustomMovieExtensions =
                new ObservableCollection<FileExtensionSettings>(userSettings.CustomMovieExtensions.Select(e => (FileExtensionSettings)e.Clone()));
        }

        private ObservableCollection<DirectoryMonitorSettings> directoryMonitors;
        public ObservableCollection<DirectoryMonitorSettings>  DirectoryMonitors
        {
            get { return directoryMonitors; }
            set { SetProperty(ref directoryMonitors, value, "DirectoryMonitors"); }
        }

        private ObservableCollection<FileExtensionSettings> defaultMovieExtensions;
        public ObservableCollection<FileExtensionSettings> DefaultMovieExtensions
        {
            get { return defaultMovieExtensions; }
            set { SetProperty(ref defaultMovieExtensions, value, "DefaultMovieExtensions"); }
        }


        private ObservableCollection<FileExtensionSettings> customMovieExtensions;
        public ObservableCollection<FileExtensionSettings> CustomMovieExtensions
        {
            get { return customMovieExtensions; }
            set { SetProperty(ref customMovieExtensions, value, "CustomMovieExtensions"); }
        }

        private bool isOpenFlyout;
        public bool IsOpenFlyout
        {
            get { return isOpenFlyout; }
            set {
                if (SetProperty(ref isOpenFlyout, value, "IsOpenFlyout"))
                {
                    if (!isOpenFlyout)
                    {
                        Rollback();
                    }
                }
            }
        }

        private string newMovieExtension;
        public string NewMovieExtension
        {
            get { return newMovieExtension; }
            set
            {
                var validChars = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();
                if (String.IsNullOrEmpty(value) || value.ToLower().ToCharArray().All(c => validChars.Contains(c)))
                {
                    SetProperty(ref newMovieExtension, value, "NewMovieExtension");
                }
            }
        }

        private ICommand toolCommand;
        public ICommand ToolCommand
        {
            get
            {
                return toolCommand ?? (toolCommand = new RelayCommand((param) => { IsOpenFlyout = true; }));
            }
        }

        private ICommand removeDirectoryMonitorCommand;
        public ICommand RemoveDirectoryMonitorCommand
        {
            get
            {
                return removeDirectoryMonitorCommand ?? (removeDirectoryMonitorCommand = new RelayCommand((param) =>
                {
                    DirectoryMonitors.Remove((DirectoryMonitorSettings)param);
                }));
            }
        }

        private ICommand addMovieExtensionCommand;
        public ICommand AddMovieExtensionCommand
        {
            get
            {
                return addMovieExtensionCommand ?? (addMovieExtensionCommand = new RelayCommand((param) =>
                {
                    var ext = NewMovieExtension?.ToLower();
                    if (!String.IsNullOrWhiteSpace(ext) &&
                        !DefaultMovieExtensions.ToList().Exists(e => e.Ext.ToLower().Equals(ext)) &&
                        !CustomMovieExtensions.ToList().Exists(e => e.Ext.ToLower().Equals(ext)))
                    {
                        CustomMovieExtensions.Add(new FileExtensionSettings()
                        {
                            Ext = NewMovieExtension,
                            IsEnabled = true
                        });
                        NewMovieExtension = "";
                    }
                }));
            }
        }

        private ICommand removeMovieExtensionCommand;
        public ICommand RemoveMovieExtensionCommand
        {
            get
            {
                return removeMovieExtensionCommand ?? (removeMovieExtensionCommand = new RelayCommand((param) =>
                {
                    CustomMovieExtensions.Remove((FileExtensionSettings)param);
                }));
            }
        }

        public ICommand CommitCommand
        {
            get { return new RelayCommand((param) => { Commit(); IsOpenFlyout = false; }); }
        }

        public ICommand RollbackCommand
        {
            get { return new RelayCommand((param) => { Rollback(); IsOpenFlyout = false; }); }
        }
    }
}
