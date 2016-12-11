using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models;
using XMovie.Models.Settings;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Logger logger = Logger.Instace;

        public ObservableCollection<MovieItemViewModel> Movies { get; private set; }

        public MovieInformationViewModel MovieInformation { get; private set; }

        public LogListViewModel Logs { get; private set; } = new LogListViewModel();

        private IDialogService dialogService;
        private DirectoryMonitor monitor;
        private MovieDispatcher movieDispatcher = new MovieDispatcher();

        private volatile bool isShutdown = false;

        public MainWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            MovieInformation = new MovieInformationViewModel(dialogService);

            Application.Current.Dispatcher.ShutdownStarted += (s, e) => { isShutdown = true; };

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

            Settings = UserSettingManager.Instance.GetUserSettings();
            Settings.ThumbnailCountChanged += ((sender, count) =>
            {
                foreach (var movieModel in Movies)
                {
                    movieModel.ThumbnailCount = count;
                }
            });

            var monitor = DirectoryMonitor.Instance;
            monitor.MovieRemoved += Monitor_MovieRemoved;
            monitor.MovieAdded += Monitor_MovieAdded;
            monitor.MovieNameChanged += Monitor_MovieNameChanged;
        }

        public void MainWindowLoaded()
        {
        }

        public void LogViewLoaded()
        {
            Logs.LogViewLoaded();

            // ログ表示のためLogViewLoadedをトリガとする
            StartMonitor();
            foreach (var ms in Settings.DirectoryMonitors.Where(dm => dm.IsBootCheckEnabled))
            {
                CheckDirectory(ms);
            }
        }

        #region Properties
        public int ThumbnailCount
        {
            get { return Settings.ThumbnailCount; }
            set
            {
                if (value < 1 || value > 5)
                {
                    SetError("thumbnailCount", "サムネイル数は1から5に設定する必要があります。");
                }
                else
                {
                    Settings.ThumbnailCount = value;
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

        public UserSettings Settings { get; set; }

        public int ThumbnailCountListIndex
        {
            get { return Settings.ThumbnailCount - 1; }
            set { Settings.ThumbnailCount = value + 1; }
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
                        await Task.Run(async () =>
                        {
                            var files = parameter as string[];
                            if (files != null)
                            {
                                var exts = UserSettingManager.Instance.GetUserSettings().GetImportableMovieExtensions(); ;
                                var importer = new MovieImporter();
                                foreach (var file in files)
                                {
                                    if (Directory.Exists(file))
                                    {
                                        // 監視ディレクトリとして追加
                                        AddDirectoryMonitor(file);
                                        continue;
                                    }
                                    if (!exts.Contains(Path.GetExtension(file).ToLower()))
                                    {
                                        logger.Warning($"対応していない拡張子です。{file}");
                                        continue;
                                    }
                                    await movieDispatcher.ImportMovie(file, Movies);
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

        private ICommand addTagCommand;
        public ICommand AddTagCommand
        {
            get
            {
                if (addTagCommand == null)
                {
                    addTagCommand = new RelayCommand((param) =>
                    {
                        var tagParameter = (TagCommandParameter)param;
                        var tag = InsertNewTag(tagParameter);

                        MovieInformation.AddTagCommand.Execute(tag);
                        foreach (MovieItemViewModel movie in MovieInformation.SelectedMovies)
                        {
                            movie.AddTagCommand.Execute(tag);
                        }
                    });
                }
                return addTagCommand;
            }
        }

        private ICommand removeTagCommand;
        public ICommand RemoveTagCommand
        {
            get
            {
                if (removeTagCommand == null)
                {
                    removeTagCommand = new RelayCommand((param) =>
                    {
                        MovieInformation.RemoveTagCommand.Execute(param);
                        foreach (MovieItemViewModel movie in MovieInformation.SelectedMovies)
                        {
                            movie.RemoveTagCommand.Execute(param);
                        }
                    });
                }
                return removeTagCommand;
            }
        }

        private ICommand removeCategoryCommand;
        public ICommand RemoveCategoryCommand
        {
            get
            {
                if (removeCategoryCommand == null)
                {
                    removeCategoryCommand = new RelayCommand(async (param) =>
                    {
                        var result = await dialogService.ShowConfirmDialog("カテゴリの削除",
                            "カテゴリを削除しますか?\n(全ての動画からカテゴリに属するすべてのタグが削除されます。)");
                        if (result)
                        {
                            MovieInformation.RemoveCategoryCommand.Execute(param);
                            foreach (MovieItemViewModel movie in MovieInformation.SelectedMovies)
                            {
                                movie.UpdateTags();
                            }
                        }
                    });
                }
                return removeCategoryCommand;
            }
        }

        private ICommand settingCommand;
        public ICommand SettingCommand
        {
            get
            {
                if (settingCommand == null)
                {
                    settingCommand = new RelayCommand((param) =>
                    {
                        dialogService.ShowSettingWindow();
                    });
                }
                return settingCommand;
            }
        }

        private ICommand unregisterMovieCommand;
        public ICommand UnregisterMovieCommand
        {
            get
            {
                if (unregisterMovieCommand == null)
                {
                    unregisterMovieCommand = new RelayCommand(async (param) =>
                    {
                        MovieItemViewModel movie = (MovieItemViewModel)param;
                        movie.IsEnabled = false;
                        await movieDispatcher.UnregisterMovie(movie.MovieId, Movies);
                    });
                }
                return unregisterMovieCommand;
            }
        }

        #endregion

        private void StartMonitor()
        {

            var monitor = DirectoryMonitor.Instance;

            monitor.StopMonitor();

            var exts = Settings.GetImportableMovieExtensions();

            monitor.StartMonitor(Settings.DirectoryMonitors, exts);
        }


        private void AddSearchHistory(string keywords)
        {
            if (Settings.SearchHistories.Contains(keywords))
            {
                Settings.SearchHistories.Move(Settings.SearchHistories.IndexOf(keywords), 0);
            }
            else
            {
                Settings.SearchHistories.Insert(0, keywords);
                if (Settings.SearchHistories.Count > 50)
                {
                    Settings.SearchHistories.RemoveAt(Settings.SearchHistories.Count - 1);
                }
            }
        }

        private void SearchMovies(string keywords)
        {
            // 検索歴の追加
            AddSearchHistory(keywords);

            var keys = new List<string>(keywords.Split(new char[] { ',', ' ', '　', '、' }, StringSplitOptions.RemoveEmptyEntries));
            if (Settings.IsFileSearch)
            {
                SearchMoviesWithPath(keys);
            }
            else
            {
                SearchMoviesWithTags(keys);
            }
        }

        private void SearchMoviesWithTags(List<string> tagKeys)
        {
            using (var context = new XMovieContext())
            {
                Movies.Clear();
                if (tagKeys.Count() > 0)
                {
                    var query = context.Tags.Join(context.TagMaps, t => t.TagId, tm => tm.TagId, (t, tm) => new { t.Name, tm.MovieId });
                    foreach (var tag in tagKeys)
                    {
                        query = query.Where(tmp => tmp.Name.Contains(tag));
                    }
                    var ids = query.Select(tmp => tmp.MovieId).ToList();
                    var movies = context.Movies.Where(m => ids.Contains(m.MovieId));
                    foreach (var movie in movies)
                    {
                        Movies.Add(new MovieItemViewModel(movie.MovieId));
                    }
                }
                else
                {
                    foreach (var movie in context.Movies)
                    {
                        Movies.Add(new MovieItemViewModel(movie.MovieId));
                    }
                }
            }
        }

        private void SearchMoviesWithPath(List<string> pathKeys)
        {
            using (var context = new XMovieContext())
            {
                Movies.Clear();
                var query = context.Movies.Select(m => m);
                // ファイル検索の場合はキーワードのlike and
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

        private Tag InsertNewTag(TagCommandParameter tagParameter)
        {
            using (var context = new XMovieContext())
            {
                // 1. 新規タグをマスタに追加
                bool isExist = context.Tags.Any(t => t.Name.Equals(tagParameter.Name) && t.TagCategoryId == tagParameter.TagCategoryId);
                Tag tag;
                if (!isExist)
                {
                    tag = new Tag()
                    {
                        TagCategoryId = tagParameter.TagCategoryId,
                        Name = tagParameter.Name
                    };
                    context.Tags.Add(tag);
                    context.SaveChanges();
                }
                else
                {
                    tag = context.Tags.Where(t => t.TagCategoryId == tagParameter.TagCategoryId && t.Name.Equals(tagParameter.Name))
                                      .Select(t => t)
                                      .FirstOrDefault();
                }
                return tag;
            }
        }

        private void AddDirectoryMonitor(string path)
        {
            var normalizedPath = Util.NormalizePath(path);
            var paths = Settings.DirectoryMonitors.Select(dm => Util.NormalizePath(dm.Path)).ToList();
            if (paths.Contains(normalizedPath))
            {
                logger.Information($"{path}は監視ディレクトリとして登録済みです。");
            }
            else
            {
                logger.Information($"{path}を監視ディレクトリとして追加しました(設定画面から解除できます)。");
                var monitorSetting = new DirectoryMonitorSettings() { Path = path };
                Settings.DirectoryMonitors.Add(monitorSetting);

                CheckDirectory(monitorSetting);
            }
        }

        private void CheckDirectory(DirectoryMonitorSettings monitorSetting)
        {
            Task.Run(async () =>
            {
                var option = monitorSetting.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                var files = Directory.EnumerateFiles(monitorSetting.Path, "*", option);
                var exts = Settings.GetImportableMovieExtensions();
                foreach (var file in files)
                {
                    if (isShutdown)
                    {
                        break;
                    }
                    if (exts.Contains(Path.GetExtension(file).ToLower())) {
                        await movieDispatcher.ImportMovie(file, Movies);
                    }
                }
            });
        }

        private void ChangeMovieName(string oldPath, string path)
        {
            using (var context = new XMovieContext())
            {
                var movies = context.Movies.ToList();
                var movie = movies.Where(m => Util.IsEqualsNormalizedPath(oldPath, m.Path)).FirstOrDefault();
                if (movie != null)
                {
                    movie.Path = path;
                    var model = Movies.Where(m => m.MovieId == movie.MovieId).FirstOrDefault();
                    if (model != null)
                    {
                        model.Path = path;
                    }
                    context.SaveChanges();
                }
            }
        }

        private void Monitor_MovieNameChanged(string oldPath, string path)
        {
            ChangeMovieName(oldPath, path);
        }

        private async void Monitor_MovieAdded(string path)
        {
            await movieDispatcher.ImportMovie(path, Movies);
        }

        private async void Monitor_MovieRemoved(string path)
        {
            List<string> movieIds = null;
            using (var context = new XMovieContext())
            {
                var list = context.Movies.Select(m => new { m.Path, m.MovieId }).ToList();
                movieIds = list.Where(tmp => Util.IsEqualsNormalizedPath(tmp.Path, path))
                               .Select(tmp => tmp.MovieId)
                               .ToList();
            }

            if (movieIds.Count() > 0)
            {
                var model = Movies.Where(m => m.MovieId.Equals(movieIds.First())).FirstOrDefault();
                if (model != null)
                {
                    System.Diagnostics.Debug.Print($"here {path}");
                    model.IsEnabled = false;
                    await movieDispatcher.UnregisterMovie(model.MovieId, Movies);
                }
            }
        }
    }
}
