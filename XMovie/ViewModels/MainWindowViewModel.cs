using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Message;
using XMovie.Models;
using XMovie.Models.Data;
using XMovie.Models.Repository;
using XMovie.Models.Settings;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private Logger logger;

        public ObservableCollection<MovieItemViewModel> Movies { get; private set; }
            = new ObservableCollection<MovieItemViewModel>();

        public MovieInformationViewModel MovieInformation { get; private set; }

        public LogListViewModel Logs { get; private set; } = new LogListViewModel();

        public ToolViewModel ToolModel { get; set; }

        private IDialogService dialogService;

        private IEventAggregator eventAggregator;

        private MovieDispatcher movieDispatcher;

        private volatile bool isShutdown = false;

        private SearchRequest searchRequest = new SearchRequest();


        public MainWindowViewModel()
        {
            logger = App.Container.Resolve<Logger>();
            eventAggregator = App.Container.Resolve<IEventAggregator>();
            dialogService = App.Container.Resolve<IDialogService>();
            movieDispatcher = App.Container.Resolve<MovieDispatcher>();

            MovieInformation = new MovieInformationViewModel();
            ToolModel = new ToolViewModel();
        }

        public void MainWindowLoaded()
        {
        }

        public async void LogViewLoaded()
        {
            Logs.LogViewLoaded();

            await Task.Run(() =>
            {
                var migration = new XMovieContextMigration();
                migration.Migration();
            });
            IsEnableMenu = true;
            OnPropertyChanged("IsEnableMenu");

            MovieInformation.Initialize();

            Subscribe();

            Application.Current.Dispatcher.ShutdownStarted += (s, e) => { isShutdown = true; };

            Settings.ThumbnailCountChanged += ((sender, count) =>
            {
                foreach (var movieModel in Movies)
                {
                    movieModel.ThumbnailCount = count;
                }
            });

            BindingOperations.EnableCollectionSynchronization(Movies, new object());

            SearchMovies("");

            var monitor = DirectoryMonitor.Instance;
            monitor.MovieRemoved += Monitor_MovieRemoved;
            monitor.MovieAdded += Monitor_MovieAdded;
            monitor.MovieNameChanged += Monitor_MovieNameChanged;

            // ログ表示のためLogViewLoadedをトリガとする
            StartMonitor();
            foreach (var ms in Settings.DirectoryMonitors.Where(dm => dm.IsBootCheckEnabled))
            {
                CheckDirectory(ms);
            }

            // MD5計算まで完了していない動画の処理
            using (var repo = new RepositoryService())
            {
                var uncompleted = repo.FindNoMD5Movies();
                var calculator = App.Container.Resolve<MD5Calculator>();
                foreach (var movie in uncompleted)
                {
                    calculator.Request(movie.MovieId);
                }
            }
        }

        private void Subscribe()
        {
            // カテゴリ削除
            eventAggregator.GetEvent<RemoveCategoryEvent>().Subscribe(tag => { RemoveCategory(); });

            // 動画登録解除
            // TODO: MovieItemViewModelでやりたくない
            eventAggregator.GetEvent<UnregisterMovieEvent>().Subscribe(async movie =>
            {
                movie.IsEnabled = false;
                await movieDispatcher.UnregisterMovie(movie.MovieId, Movies);
                foreach (MovieItemViewModel m in MovieInformation.SelectedMovies)
                {
                    m.IsEnabled = false;
                    await movieDispatcher.UnregisterMovie(m.MovieId, Movies);
                }
            }, ThreadOption.UIThread);

            // 動画移動
            eventAggregator.GetEvent<MoveMovieEvent>().Subscribe(movie => { MoveMovie(movie); });

            // 動画削除
            eventAggregator.GetEvent<RemoveMovieEvent>().Subscribe(movie => { RemoveMovie(movie); });

            // 検索
            eventAggregator.GetEvent<SearchEvent>().Subscribe(keyword =>
            {
                SearchKeywords = keyword;
                SearchMovies(keyword);
            });
        }

        private void RemoveCategory()
        {
            if (MovieInformation.SelectedMovies != null)
            {
                foreach (MovieItemViewModel movie in MovieInformation.SelectedMovies)
                {
                    movie.UpdateTags();
                }
            }
        }

        private void MoveMovie(MovieItemViewModel movie)
        {
            var dest = dialogService.ShowFolderDialog("移動先フォルダの選択", Path.GetDirectoryName(movie.Path));
            if (dest == null)
            {
                return;
            }
            try
            {
                dest = Path.Combine(dest, movie.FileName);
                DirectoryMonitor.Instance.PauseMonitor();
                File.Move(movie.Path, dest);
                logger.Information($"ファイルを移動しました。{movie.Path} -> {dest}");
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

            movie.Path = dest;

            using (var repo = new RepositoryService())
            {
                repo.ApplyMovie(movie.MovieId, (m => m.Path = dest));
            }
        }

        private async void RemoveMovie(MovieItemViewModel movie)
        {
            var msg = $"{movie.Path}を削除しますか?\n(ファイルは完全に削除されます。)";
            if (await dialogService.ShowConfirmDialog("ファイルの削除", msg))
            {
                movie.IsEnabled = false;
                await movieDispatcher.UnregisterMovie(movie.MovieId, Movies);
                try
                {
                    DirectoryMonitor.Instance.PauseMonitor();
                    File.Delete(movie.Path);
                    logger.Information($"ファイルを削除しました。{movie.Path}");
                }
                catch (Exception ex)
                {
                    logger.Error($"ファイルが削除できませんでした。{movie.Path}");
                    logger.Error(ex);
                    return;
                }
                finally
                {
                    DirectoryMonitor.Instance.ResumeMonitor();
                }
            }
        }
        #region Properties

        public bool IsEnableMenu { get; set; } = false;

        public ObservableCollection<SearchTagMenuItemViewModel> SearchTags
        {
            get
            {
                return SearchTagMenuItemViewModel.CreateTree()?.MenuItems;
            }
        }

        public int ThumbnailCount
        {
            get { return Settings.ThumbnailCount; }
            set
            {
                if (value < 1 || value > 5)
                {
                    //SetError("thumbnailCount", "サムネイル数は1から5に設定する必要があります。");
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

        public UserSettings Settings { get; set; } = UserSettingManager.Instance.GetUserSettings();

        public int ThumbnailCountListIndex
        {
            get { return Settings.ThumbnailCount - 1; }
            set { Settings.ThumbnailCount = value + 1; }
        }

        private bool isSearching;
        public bool IsSearching
        {
            get { return isSearching; }
            set { SetProperty(ref isSearching, value, "IsSearching"); }
        }
        #endregion

        #region Command
        private ICommand windowClosingCommand;
        public ICommand WindowClosingCommand
        {
            get
            {
                return windowClosingCommand ?? (windowClosingCommand = new DelegateCommand(() => { UserSettingManager.Instance.Save(); }));
            }
        }

        private ICommand fileDropCommand;
        public ICommand FileDropCommand
        {
            get
            {
                return fileDropCommand ?? (fileDropCommand = new DelegateCommand<string[]>(async (files) =>
                {
                    await Task.Run(async () =>
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
                    });
                }));
            }
        }

        private ICommand searchCommand;
        public ICommand SearchCommand
        {
            get
            {
                return searchCommand ?? (searchCommand = new DelegateCommand<string>((param) => { SearchMovies(param); }));
            }
        }

        private ICommand tagSearchCommand;
        public ICommand TagSearchCommand
        {
            get
            {
                return tagSearchCommand ?? (tagSearchCommand = new DelegateCommand<string>((keyword) =>
                {
                    SearchKeywords = keyword;
                    SearchMovies(SearchKeywords);
                }));
            }
        }

        private ICommand removeUnusedTagCommand;
        public ICommand RemoveUnusedTagCommand
        {
            get
            {
                return removeUnusedTagCommand ?? (removeUnusedTagCommand = new DelegateCommand(async () =>
                {
                    using (var repos = new RepositoryService())
                    {
                        var tags = repos.FindUnusedTags();
                        if (tags.Count() > 0)
                        {
                            var tagStrings = String.Join(",", tags.Select(t => t.Name));
                            var msg = $"使用されていないタグを削除しますか?\n[{tagStrings}]";
                            if (await dialogService.ShowConfirmDialog("未使用タグの削除", msg))
                            {
                                repos.RemoveTags(tags);
                                foreach (var t in MovieInformation.Tags)
                                {
                                    t.UpdateCategoryTags();
                                }
                            }
                        }
                        else
                        {
                            var msg = "未使用のタグがありません。";
                            await dialogService.ShowMessageDialog("未使用タグの削除", msg);
                        }
                    }
                }));
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

        private string searchKeywords;
        private string SearchKeywords
        {
            get { return searchKeywords; }
            set { SetProperty(ref searchKeywords, value, "SearchKeywords"); }
        }

        private void AddSearchHistory(string keywords)
        {
            if (Settings.SearchHistories.Contains(keywords))
            {
                if (Settings.SearchHistories.IndexOf(keywords) > 0)
                {
                    Settings.SearchHistories.Move(Settings.SearchHistories.IndexOf(keywords), 0);
                }
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

        private async void SearchMovies(string keywords)
        {
            var cond = $"{keywords}:{Settings.SorterIndex}";
            if (lastSearchCondition.Equals(cond))
                return;
            lastSearchCondition = cond;

            // 検索歴の追加
            AddSearchHistory(keywords);

            var sort = Sorter[Settings.SorterIndex];
            if ("cmd:duplicate".Equals(keywords))
            {
                await SearchDuplicateMovies(sort);
            }
            else
            {
                var keys = new List<string>((keywords ?? "").Split(new char[] { ',', ' ', '　', '、' }, StringSplitOptions.RemoveEmptyEntries));

                var entry = searchRequest.CreateSearchEntry();
                await Task.Run(async () =>
                {
                    try
                    {
                        await searchRequest.Entry(entry);
                        Movies.Clear();
                        IsSearching = true;
                        using (var repo = new RepositoryService())
                        {
                            var movies = repo.FindMovies(keys, sort);
                            logger.Information($"{movies.Count()}件見つかりました。[{String.Join(",", keys)}]");
                            foreach (var movie in movies)
                            {
                                Movies.Add(new MovieItemViewModel(movie.MovieId));
                                if (searchRequest.IsCancelled(entry))
                                {
                                    logger.Debug($"検索処理がキャンセルされました。{entry}");
                                    break;
                                }
                                await Task.Delay(40);
                            }
                        }
                    }
                    finally
                    {
                        IsSearching = false;
                        searchRequest.Release(entry);
                    }
                });
            }
        }

        private async Task SearchDuplicateMovies(SortDescriptor sort)
        {
            var entry = searchRequest.CreateSearchEntry();
            await Task.Run(async () =>
            {
                try
                {
                    await searchRequest.Entry(entry);
                    IsSearching = true;
                    Movies.Clear();

                    using (var repo = new RepositoryService())
                    {
                        foreach (var movie in repo.FindDuplicateMovies(sort))
                        {
                            Movies.Add(new MovieItemViewModel(movie.MovieId));
                            await Task.Delay(40);
                            if (searchRequest.IsCancelled(entry))
                            {
                                logger.Debug($"検索処理がキャンセルされました。{entry}");
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    IsSearching = false;
                    searchRequest.Release(entry);
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
