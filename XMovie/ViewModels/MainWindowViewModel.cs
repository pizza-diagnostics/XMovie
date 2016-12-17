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
using XMovie.Models.Repository;
using XMovie.Models.Settings;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Logger logger = Logger.Instace;

        public ObservableCollection<MovieItemViewModel> Movies { get; private set; }
            = new ObservableCollection<MovieItemViewModel>();

        public MovieInformationViewModel MovieInformation { get; private set; }

        public LogListViewModel Logs { get; private set; } = new LogListViewModel();

        private IDialogService dialogService;
        private MovieDispatcher movieDispatcher = new MovieDispatcher();

        private volatile bool isShutdown = false;

        public MainWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            MovieInformation = new MovieInformationViewModel(dialogService);

            Application.Current.Dispatcher.ShutdownStarted += (s, e) => { isShutdown = true; };

            Settings = UserSettingManager.Instance.GetUserSettings();
            Settings.ThumbnailCountChanged += ((sender, count) =>
            {
                foreach (var movieModel in Movies)
                {
                    movieModel.ThumbnailCount = count;
                }
            });

            SearchMovies("");

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

        private List<SortDescriptor> sorter;
        public List<SortDescriptor> Sorter
        {
            get
            {
                if (sorter == null)
                {
                    sorter = new List<SortDescriptor>(new SortDescriptor[]
                    {
                        new SortDescriptor<int>()
                        {
                            Title = "再生回数昇順",
                            IsAsc = true,
                            Column = "PlayCount",
                            MovieSortFunc = (m => m.PlayCount)
                        },
                        new SortDescriptor<int>()
                        {
                            Title = "再生回数降順",
                            IsAsc = false,
                            Column = "PlayCount",
                            MovieSortFunc = (m => m.PlayCount)
                        },
                        new SortDescriptor<int>()
                        {
                            Title = "ランク昇順",
                            IsAsc = true,
                            Column = "Rank",
                            MovieSortFunc = (m => m.Rank)
                        },
                        new SortDescriptor<int>()
                        {
                            Title = "ランク降順",
                            IsAsc = false,
                            Column = "Rank",
                            MovieSortFunc = (m => m.Rank)
                        },
                        new SortDescriptor<DateTime>()
                        {
                            Title = "登録日時昇順",
                            IsAsc = true,
                            Column = "RegisteredDate",
                            MovieSortFunc = (m => m.RegisteredDate)
                        },
                        new SortDescriptor<DateTime>()
                        {
                            Title = "登録日時降順",
                            IsAsc = false,
                            Column = "RegisteredDate",
                            MovieSortFunc = (m => m.RegisteredDate)
                        },
                        new SortDescriptor<DateTime>()
                        {
                            Title = "ファイル作成日時昇順",
                            IsAsc = true,
                            Column = "FileCreateDate",
                            MovieSortFunc = (m => m.FileCreateDate)
                        },
                        new SortDescriptor<DateTime>()
                        {
                            Title = "ファイル作成日時降順",
                            IsAsc = false,
                            Column = "FileCreateDate",
                            MovieSortFunc = (m => m.FileCreateDate)
                        },
                        new SortDescriptor<DateTime>()
                        {
                            Title = "ファイル更新日時昇順",
                            IsAsc = true,
                            Column = "FileModifiedDate",
                            MovieSortFunc = (m => m.FileModifiedDate)
                        },
                        new SortDescriptor<DateTime>()
                        {
                            Title = "ファイル更新日時降順",
                            IsAsc = false,
                            Column = "FileModifiedDate",
                            MovieSortFunc = (m => m.FileModifiedDate)
                        },
                    });
                }
                return sorter;
            }
        }

        private string lastSearchCondition = "cmd:initial:-1";

        public UserSettings Settings { get; set; }

        public int ThumbnailCountListIndex
        {
            get { return Settings.ThumbnailCount - 1; }
            set { Settings.ThumbnailCount = value + 1; }
        }

        public bool IsFileSearch
        {
            get { return Settings.IsFileSearch; }
            set
            {
                bool v = IsFileSearch;
                Settings.IsFileSearch = value;
                SetProperty(ref v, value, "IsFileSearch");
            }
        }

        private bool isSearching;
        public bool IsSearching
        {
            get { return isSearching; }
            set { SetProperty(ref isSearching, value, "IsSearching"); }
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

        private ICommand tagSearchCommand;
        public ICommand TagSearchCommand
        {
            get
            {
                if (tagSearchCommand == null)
                {
                    tagSearchCommand = new RelayCommand((param) =>
                    {
                        IsFileSearch = false;
                        SearchMovies(((Tag)param).Name);
                    });
                }
                return tagSearchCommand;
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
                        if (String.IsNullOrWhiteSpace(tagParameter.Name))
                            return;

                        using (var repos = new RepositoryService())
                        {
                            var tag = repos.InsertNewTag(tagParameter.Name, tagParameter.TagCategoryId);
                            MovieInformation.AddTagCommand.Execute(tag);
                            foreach (MovieItemViewModel movie in MovieInformation.SelectedMovies)
                            {
                                movie.AddTagCommand.Execute(tag);
                            }
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
                            if (MovieInformation.SelectedMovies != null)
                            {
                                foreach (MovieItemViewModel movie in MovieInformation.SelectedMovies)
                                {
                                    movie.UpdateTags();
                                }

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
                        foreach (MovieItemViewModel m in MovieInformation.SelectedMovies)
                        {
                            m.IsEnabled = false;
                            await movieDispatcher.UnregisterMovie(m.MovieId, Movies);
                        }

                    });
                }
                return unregisterMovieCommand;
            }
        }

        private ICommand moveMovieCommand;
        public ICommand MoveMovieCommand
        {
            get
            {
                if (moveMovieCommand == null)
                {
                    moveMovieCommand = new RelayCommand((param) =>
                    {
                        var model = (MovieItemViewModel)param;
                        var dest = dialogService.ShowFolderDialog("移動先フォルダの選択", Path.GetDirectoryName(model.Path));
                        if (dest == null)
                        {
                            return;
                        }
                        try
                        {
                            dest = Path.Combine(dest, model.FileName);
                            DirectoryMonitor.Instance.PauseMonitor();
                            File.Move(model.Path, dest);
                            logger.Information($"ファイルを移動しました。{model.Path} -> {dest}");
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                            return;
                        }
                        finally
                        {
                            DirectoryMonitor.Instance.ResumeMonitor();
                        }

                        model.Path = dest;

                        using (var repo = new RepositoryService())
                        {
                            repo.ApplyMovie(model.MovieId, (m => m.Path = dest));
                        }
                        /*
                        var movieRepository = new MovieRepository();
                        movieRepository.UpdateMovie(model.MovieId, (m => m.Path = dest));
                        */

                    });
                }
                return moveMovieCommand;
            }
        }

        private ICommand removeMovieCommand;
        public ICommand RemoveMovieCommand
        {
            get
            {
                if (removeMovieCommand == null)
                {
                    removeMovieCommand = new RelayCommand(async (param) =>
                    {
                        var model = (MovieItemViewModel)param;
                        var msg = $"{model.Path}を削除しますか?\n(ファイルは完全に削除されます。)";
                        if (await dialogService.ShowConfirmDialog("ファイルの削除", msg))
                        {
                            model.IsEnabled = false;
                            await movieDispatcher.UnregisterMovie(model.MovieId, Movies);
                            try
                            {
                                DirectoryMonitor.Instance.PauseMonitor();
                                File.Delete(model.Path);
                                logger.Information($"ファイルを削除しました。{model.Path}");
                            }
                            catch (Exception ex)
                            {
                                logger.Error($"ファイルが削除できませんでした。{model.Path}");
                                logger.Error(ex);
                                return;
                            }
                            finally
                            {
                                DirectoryMonitor.Instance.ResumeMonitor();
                            }
                        }
                    });
                }
                return removeMovieCommand;
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

        public string SearchKeywords
        {
            get { return Settings.SearchHistories.Count > 0 ? Settings.SearchHistories[0] : ""; }
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
            OnPropertyChanged("SearchKeywords");
        }

        private async void SearchMovies(string keywords)
        {
            var cond = $"{keywords}:{Settings.SorterIndex}";
            if (lastSearchCondition.Equals(cond))
                return;
            lastSearchCondition = cond;

            // 検索歴の追加
            AddSearchHistory(keywords);

            IsSearching = true;

            var sort = Sorter[Settings.SorterIndex];
            if ("cmd:duplicate".Equals(keywords))
            {
                SearchDuplicateMovies(sort);
            }
            else
            {
                var keys = new List<string>(keywords.Split(new char[] { ',', ' ', '　', '、' }, StringSplitOptions.RemoveEmptyEntries));

                if (IsFileSearch)
                {
                    await SearchMoviesWithPath(keys, sort);
                }
                else
                {
                    await SearchMoviesWithTags(keys, sort);
                }
            }
            IsSearching = false;

        }

        private void SearchDuplicateMovies(SortDescriptor sort)
        {
            Movies.Clear();
            using (var repo = new RepositoryService())
            {
                foreach (var movie in repo.FindDuplicateMovies(sort))
                {
                    Movies.Add(new MovieItemViewModel(movie.MovieId));
                }
            }
        }

        private async Task SearchMoviesWithTags(List<string> tagKeys, SortDescriptor sort)
        {
            Movies.Clear();
            await Task.Run(async () =>
            {
                using (var repos = new RepositoryService())
                {
                    var movies = repos.FindMoviesByTags(tagKeys, sort);
                    logger.Information($"{movies.Count()}件見つかりました。[{String.Join(",", tagKeys)}]");
                    foreach (var movie in movies)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Movies.Add(new MovieItemViewModel(movie.MovieId));
                        });
                        await Task.Delay(5);
                    }
                }
            });
        }

        private async Task SearchMoviesWithPath(List<string> pathKeys, SortDescriptor sort)
        {
            Movies.Clear();
            await Task.Run(async () =>
            {
                using (var repo = new RepositoryService())
                {
                    var movies = repo.FindMoviesByPathKeys(pathKeys, sort);
                    logger.Information($"{movies.Count()}件見つかりました。[{String.Join(",", pathKeys)}]");
                    foreach (var movie in movies)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Movies.Add(new MovieItemViewModel(movie.MovieId));
                        });
                        await Task.Delay(5);
                    }
                }
            });
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
                App.Current.Dispatcher.Invoke(() =>
                {
                    Settings.DirectoryMonitors.Add(monitorSetting);
                });

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
            using (var repos = new RepositoryService())
            {
                var movie = repos.FindMovieAtPath(oldPath);

                if (movie != null)
                {
                    movie.Path = path;
                    repos.UpdateMovie(movie);

                    var model = Movies.Where(m => m.MovieId == movie.MovieId).FirstOrDefault();
                    if (model != null)
                    {
                        model.Path = path;
                    }
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
            using (var repos = new RepositoryService())
            {
                var movie = repos.FindMovieAtPath(path);
                if (movie != null)
                {
                    var model = Movies.Where(m => m.MovieId.Equals(movie.MovieId)).FirstOrDefault();
                    if (model != null)
                    {
                        model.IsEnabled = false;
                        await movieDispatcher.UnregisterMovie(model.MovieId, Movies);
                    }
                }
            }
        }
    }
}
