﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models;
using XMovie.Models.Settings;
using static System.Diagnostics.Debug;

namespace XMovie.ViewModels
{
    public class MovieListViewModel : ViewModelBase
    {
        public ObservableCollection<MovieItemViewModel> Movies { get; private set; }

        private RelayCommand fileDropCommand;

        private Logger logger = Logger.Instace;

        private UserSettings userSettings;

        public ICommand FileDropCommand
        {
            get
            {
                if (fileDropCommand == null)
                {
                    fileDropCommand = new RelayCommand(FileDropAction);
                }
                return fileDropCommand;
            }
        }

        public MovieListViewModel()
        {
            using (var context = new XMovieContext())
            {
                // TODO: ViewModelの渡し方が違う？
                Movies = new ObservableCollection<MovieItemViewModel>();
                foreach (var movie in context.Movies)
                {
                    Movies.Add(new MovieItemViewModel(movie.MovieId));
                }
            }

            userSettings = UserSettingManager.Instance.GetUserSettings();
            userSettings.ThumbnailCountChanged += ((sender, count) =>
            {
                foreach (var movieModel in Movies)
                {
                    movieModel.ThumbnailCount = count;
                }
            });
        }

        private async void FileDropAction(object parameter)
        {
            await Task.Run(() =>
            {
                var files = parameter as string[];
                if (files != null)
                {
                    var importer = new MovieImporter();
                    foreach (var file in files)
                    {
                        try
                        {
                            var movie = importer.Import(file);
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                using (var context = new XMovieContext())
                                {
                                    context.Movies.Add(movie);
                                    foreach (var thumbnail in movie.Thumbnails)
                                    {
                                        context.Thumbnails.Add(thumbnail);
                                    }
                                    context.SaveChanges();
                                }
                                Movies.Add(new MovieItemViewModel(movie.MovieId));
                            });
                        }
                        catch (MovieImporterException ex)
                        {
                            if (ex.Reason == MovieImporterException.Error.FFProbeError)
                            {
                                logger.Warning(ex);
                            }
                            else
                            {
                                logger.Error(ex);
                            }
                        }
                    }
                }
            });
        }
    }
}
