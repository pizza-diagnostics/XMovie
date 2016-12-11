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
    public class SettingWindowViewModel : ViewModelBase
    {
        private UserSettings userSettings = UserSettingManager.Instance.GetUserSettings();
        private IDialogService dialogSerivice;

        public SettingWindowViewModel(IDialogService service)
        {
            dialogSerivice = service;
            defaultMovieExtensions =
                new ObservableCollection<FileExtensionSettings>(userSettings.DefaultMovieExtensions.Select(e => (FileExtensionSettings)e.Clone()));
            customMovieExtensions =
                new ObservableCollection<FileExtensionSettings>(userSettings.CustomMovieExtensions.Select(e => (FileExtensionSettings)e.Clone()));
            directoryMonitors =
                new ObservableCollection<DirectoryMonitorSettings>(userSettings.DirectoryMonitors.Select(m => (DirectoryMonitorSettings)m.Clone()));
        }

        private ObservableCollection<FileExtensionSettings> defaultMovieExtensions;
        public ObservableCollection<FileExtensionSettings> DefaultMovieExtensions
        {
            get { return defaultMovieExtensions; }
            set { defaultMovieExtensions = value; }
        }


        private ObservableCollection<FileExtensionSettings> customMovieExtensions;
        public ObservableCollection<FileExtensionSettings> CustomMovieExtensions
        {
            get { return customMovieExtensions; }
            set { customMovieExtensions = value; }
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

        private ObservableCollection<DirectoryMonitorSettings> directoryMonitors;
        public ObservableCollection<DirectoryMonitorSettings>  DirectoryMonitors
        {
            get { return directoryMonitors; }
            set { directoryMonitors = value; }
        }

        public void Commit()
        {
            userSettings.DefaultMovieExtensions = DefaultMovieExtensions;
            userSettings.CustomMovieExtensions = CustomMovieExtensions;
            userSettings.DirectoryMonitors = DirectoryMonitors;
        }

        public ICommand SaveCommand
        {
            get { return new RelayCommand((param) => { Commit(); }); }
        }

        private ICommand addMovieExtensionCommand;
        public ICommand AddMovieExtensionCommand
        {
            get
            {
                if (addMovieExtensionCommand == null)
                {
                    addMovieExtensionCommand = new RelayCommand((param) =>
                    {
                        if (!CustomMovieExtensions.ToList().Exists(e => e.Ext.Equals(NewMovieExtension)))
                        {
                            CustomMovieExtensions.Add(new FileExtensionSettings()
                            {
                                Ext = NewMovieExtension,
                                IsEnabled = true
                            });
                            NewMovieExtension = "";
                        }

                    });
                }
                return addMovieExtensionCommand;
            }
        }

        private ICommand removeMovieExtensionCommand;
        public ICommand RemoveMovieExtensionCommand
        {
            get
            {
                if (removeMovieExtensionCommand == null)
                {
                    removeMovieExtensionCommand = new RelayCommand((param) =>
                    {
                        CustomMovieExtensions.Remove((FileExtensionSettings)param);
                    });
                }
                return removeMovieExtensionCommand;
            }
        }

        private ICommand removeDirectoryMonitorCommand;
        public ICommand RemoveDirectoryMonitorCommand
        {
            get
            {
                if (removeDirectoryMonitorCommand == null)
                {
                    removeDirectoryMonitorCommand = new RelayCommand((param) =>
                    {
                        DirectoryMonitors.Remove((DirectoryMonitorSettings)param);
                    });
                }
                return removeDirectoryMonitorCommand;
            }
        }

    }
}
