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
                        new SortDescriptor<int>() { Title = "再生回数昇順", IsAsc = true, MovieSortFunc = (m => m.PlayCount) },
                        new SortDescriptor<int>() { Title = "再生回数降順", IsAsc = false, MovieSortFunc = (m => m.PlayCount) },
                        new SortDescriptor<int>() { Title = "ランク昇順", IsAsc = true, MovieSortFunc = (m => m.Rank) },
                        new SortDescriptor<int>() { Title = "ランク降順", IsAsc = false, MovieSortFunc = (m => m.Rank) },
                        new SortDescriptor<DateTime>() { Title = "登録日時昇順", IsAsc = true, MovieSortFunc = (m => m.RegisteredDate) },
                        new SortDescriptor<DateTime>() { Title = "登録日時降順", IsAsc = false, MovieSortFunc = (m => m.RegisteredDate) },
                        new SortDescriptor<DateTime>() { Title = "ファイル作成日時昇順", IsAsc = true, MovieSortFunc = (m => m.FileCreateDate) },
                        new SortDescriptor<DateTime>() { Title = "ファイル作成日時降順", IsAsc = false, MovieSortFunc = (m => m.FileCreateDate) },
                        new SortDescriptor<DateTime>() { Title = "ファイル更新日時昇順", IsAsc = true, MovieSortFunc = (m => m.FileModifiedDate) },
                        new SortDescriptor<DateTime>() { Title = "ファイル更新日時降順", IsAsc = false, MovieSortFunc = (m => m.FileModifiedDate) },
                    });
                }
                return sorter;
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
                        using (var context = new XMovieContext())
                        {
                            var movie = context.Movies.Where(m => m.MovieId == model.MovieId).FirstOrDefault();
                            if (movie != null)
                            {
                                movie.Path = dest;
                            }
                            context.SaveChanges();
                        }
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

        private void SearchMovies(string keywords)
        {
            // 検索歴の追加
            AddSearchHistory(keywords);

            var sort = Sorter[Settings.SorterIndex];
            if ("cmd:duplicate".Equals(keywords))
            {
                SearchDuplicateMovies(sort);
            }
            else
            {
                var keys = new List<string>(keywords.Split(new char[] { ',', ' ', '　', '、' }, StringSplitOptions.RemoveEmptyEntries));

                if (Settings.IsFileSearch)
                {
                    SearchMoviesWithPath(keys, sort);
                }
                else
                {
                    SearchMoviesWithTags(keys, sort);
                }
            }

        }

        private void SearchDuplicateMovies(SortDescriptor sort)
        {
            using (var context = new XMovieContext())
            {
                Movies.Clear();
                var md5list = from m in context.Movies
                              group m by m.MD5Sum into grouped
                              where grouped.Count() > 1
                              select grouped.Key;
                var movies = sort.MovieSort(context.Movies.Where(m => md5list.Contains(m.MD5Sum)));
                foreach (var movie in movies)
                {
                    Movies.Add(new MovieItemViewModel(movie.MovieId));
                }
            }
        }

        private void SearchMoviesWithTags(List<string> tagKeys, SortDescriptor sort)
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
                    var movies = sort.MovieSort(context.Movies.Where(m => ids.Contains(m.MovieId)));
                    foreach (var movie in movies)
                    {
                        Movies.Add(new MovieItemViewModel(movie.MovieId));
                    }
                }
                else
                {
                    foreach (var movie in sort.MovieSort(context.Movies))
                    {
                        Movies.Add(new MovieItemViewModel(movie.MovieId));
                    }
                }
            }
        }

        private void SearchMoviesWithPath(List<string> pathKeys, SortDescriptor sort)
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
                query = sort.MovieSort(query);
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
                    model.IsEnabled = false;
                    await movieDispatcher.UnregisterMovie(model.MovieId, Movies);
                }
            }
        }
    }
}
