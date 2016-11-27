using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private Logger logger = Logger.Instace;

        public ObservableCollection<MovieItemViewModel> Movies { get; private set; }

        public MainWindowViewModel()
        {
            using (var context = new XMovieContext())
            {
                // TODO: ViewModelの渡し方が違う？
                // TODO: Backgroundでやらないと
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

        public void MainWindowLoaded()
        {
        }

        #region Properties
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

        public bool IsFileSearch
        {
            get { return userSettings.IsFileSearch; }
            set { userSettings.IsFileSearch = value; }
        }
        #endregion

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

        private RelayCommand fileDropCommand;
        public ICommand FileDropCommand
        {
            get
            {
                if (fileDropCommand == null)
                {
                    fileDropCommand = new RelayCommand(async (parameter) =>
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

                    });
                }
                return fileDropCommand;
            }
        }

        private RelayCommand searchCommand;
        public ICommand SearchCommand
        {
            get
            {
                if (searchCommand == null)
                {
                    searchCommand = new RelayCommand((parameter) =>
                    {
                        SearchMovies((string)parameter);
                    });
                }
                return searchCommand;
            }
        }
        #endregion

        private void SearchMovies(string keywords)
        {
            // TODO: 検索歴の追加

            var keys = new List<string>(keywords.Split(new char[] { ',', ' ', '　', '、' }, StringSplitOptions.RemoveEmptyEntries));
            if (IsFileSearch)
            {
                SearchMoviesWithPath(keys);
            }
            else
            {
                // TODO: タグ検索
            }
        }

        private void SearchMoviesWithPath(List<string> pathKeys)
        {
            using (var context = new XMovieContext())
            {
                Movies.Clear();
                var query = context.Movies.Select(m => m);
                foreach (var path in pathKeys)
                {
                    query = query.Where(m => m.Path.Contains(path));
                }
                var movies = query.ToList<Movie>();
                foreach (var movie in movies)
                {
                    Movies.Add(new MovieItemViewModel(movie.MovieId));
                }
            }
        }
    }
}
