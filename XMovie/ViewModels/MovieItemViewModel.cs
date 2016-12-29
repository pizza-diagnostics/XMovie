using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XMovie.Common;
using XMovie.Models;
using XMovie.Models.Data;
using XMovie.Models.Repository;
using XMovie.Models.Settings;

namespace XMovie.ViewModels
{
    public class MovieItemViewModel : BindableBase
    {
        private Logger logger = Logger.Instace;

        public MovieItemViewModel()
        {
            ThumbnailCount = UserSettingManager.Instance.GetUserSettings().ThumbnailCount;
        }

        public MovieItemViewModel(string movieId) : this()
        {
            MovieId = movieId;
            using (var repos = new RepositoryService())
            {
                var movie = repos.FindMovie(movieId);
                Rank = movie.Rank;
                PlayCount = movie.PlayCount;
                Path = movie.Path;
            }
            UpdateTags();
        }

        public string MovieId { get; private set; }

        public bool IsEnabled { get; set; } = true;

        private bool isRenameMode;
        public bool IsRenameMode
        {
            get { return isRenameMode; }
            set { SetProperty(ref isRenameMode, value, "IsRenameMode"); }
        }

        public string EditFileName
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(FileName); }
        }

        private ObservableCollection<Tag> tags;
        public ObservableCollection<Tag> Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value, "Tags"); }
        }

        private int rank;
        public int Rank
        {
            get { return rank; }
            set { SetProperty(ref rank, value, "Rank"); }
        }

        private int playCount;
        public int PlayCount
        {
            get { return playCount; }
            set { SetProperty(ref playCount, value, "PlayCount"); }
        }

        private string path;
        public string Path
        {
            get { return path; }
            set {
                if (SetProperty(ref path, value, "Path"))
                {
                    OnPropertyChanged("FileName");
                    OnPropertyChanged("EditFileName");
                }
            }
        }

        public string FileName
        {
            get { return System.IO.Path.GetFileName(Path); }
        }

        private int thumbnailCount;
        public int ThumbnailCount
        {
            get { return thumbnailCount; }
            set
            {
                if (value >= 1 || value <= 5)
                {
                    if (SetProperty(ref thumbnailCount, value, "thumbnailCount"))
                    {
                        OnPropertyChanged("ThumbnailImage");
                        OnPropertyChanged("ThumbnailWidth");
                        OnPropertyChanged("MovieItemWidth");
                    }
                }
            }
        }

        public int MovieItemWidth
        {
            get { return ThumbnailWidth + 10; }
        }

        public int MovieItemHeight
        {
            get { return 200; }
        }

        public int ThumbnailHeight
        {
            get { return 120; }
        }
        
        public int ThumbnailWidth
        {
            get
            {
                var widthTable = new int[]
                {
                    160, 320, 480, 600, 760
                };

                return widthTable[ThumbnailCount - 1];
            }
        }

        public ImageSource ThumbnailImage
        {
            get
            {
                try
                {
                    IList<Thumbnail> thumbnails = null;
                    using (var repos = new RepositoryService())
                    {
                        thumbnails = repos.FindMovieThumbnails(MovieId);
                    }
                    if (thumbnails == null || thumbnails.Count() == 0)
                        return null;

                    var visual = new DrawingVisual();
                    var x = 0;
                    var y = 0;
                    using (var dc = visual.RenderOpen())
                    {
                        foreach (var item in thumbnails.Select((t, i) => new { t, i }))
                        {
                            if (item.i >= thumbnailCount)
                            {
                                break;
                            }

                            BitmapSource bitmapImage;
                            if (File.Exists(item.t.Path))
                            {
                                using (var stream = new FileStream(item.t.Path, FileMode.Open, FileAccess.Read))
                                {
                                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                                    bitmapImage = decoder.Frames[0];
                                }

                                dc.DrawImage(bitmapImage, new Rect(x, 0, bitmapImage.PixelWidth, bitmapImage.PixelHeight));
                                y = bitmapImage.PixelHeight; // 固定
                                x += bitmapImage.PixelWidth;
                            }
                            else
                            {
                                logger.Error($"サムネイル画像が見つかりません。{item.t.Path}");
                            }
                        }
                    }
                    if (x > 0 && y > 0)
                    {
                        var bitmap = new RenderTargetBitmap(x, y, 96, 96, PixelFormats.Pbgra32);
                        bitmap.Render(visual);
                        return bitmap;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    return null;
                }
            }
        }

        public ObservableCollection<SearchTagMenuItemViewModel> AllTags
        {
            get
            {
                return SearchTagMenuItemViewModel.CreateTree(MovieId)?.MenuItems;
            }
        }
        public void UpdateTags()
        {
            using (var repos = new RepositoryService())
            {
                Tags = new ObservableCollection<Tag>(repos.FindMovieTags(new string[] { MovieId }));
            }
        }

        #region Command
        private ICommand changeRankCommand;
        public ICommand ChangeRankCommand
        {
            get
            {
                return changeRankCommand ?? (changeRankCommand = new DelegateCommand<int?>((rankDelta) =>
                {
                    using (var repos = new RepositoryService())
                    {
                        var movie = repos.ApplyMovie(MovieId, (m => m.Rank += rankDelta.Value));
                        if (movie != null)
                        {
                            Rank = movie.Rank;
                        }
                    }
                }));
            }
        }

        private ICommand playMovieCommand;
        public ICommand PlayMovieCommand
        {
            get
            {
                return playMovieCommand ?? (playMovieCommand = new DelegateCommand(() =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(Path);
                        using (var repos = new RepositoryService())
                        {
                            var movie = repos.ApplyMovie(MovieId, (m => ++m.PlayCount));
                            if (movie != null)
                            {
                                PlayCount = movie.PlayCount;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }));
            }
        }

        private ICommand addTagCommand;
        public ICommand AddTagCommand
        {
            get
            {
                return addTagCommand ?? (addTagCommand = new DelegateCommand<object>((param) =>
                {
                    var tagParam = param as Tag;
                    if (tagParam == null)
                    {
                        var tag = ((SearchTagMenuItemViewModel)param).Tag;
                        using (var repos = new RepositoryService())
                        {
                            repos.SetTagToMovies(tag, new string[] { MovieId });
                        }
                        UpdateTags();
                    }
                    else
                    {
                        var isExist = Tags.Any(t => t.TagId == tagParam.TagId);
                        if (!isExist)
                        {
                            // TODO: 冗長
                            UpdateTags();
                        }
                    }
                }));
            }
        }

        private ICommand removeTagCommand;
        public ICommand RemoveTagCommand
        {
            get
            {
                return removeTagCommand ?? (removeTagCommand = new DelegateCommand<Tag>((tag) =>
                {
                    var remove = Tags.Where(t => t.TagId == (tag).TagId).FirstOrDefault();
                    Tags.Remove(remove);
                }));
            }
        }

        private ICommand beginRenameCommand;
        public ICommand BeginRenameCommand
        {
            get
            {
                return beginRenameCommand ?? (beginRenameCommand = new DelegateCommand(() => { IsRenameMode = true; }));
            }
        }

        private ICommand renameCommand;
        public ICommand RenameCommand
        {
            get
            {
                return renameCommand ?? (renameCommand = new DelegateCommand<string>((newFileName) =>
                {
                    var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                    if (newFileName.Any(c => invalidChars.Contains(c)))
                    {
                        logger.Error("ファイル名に使用できない文字が含まれています。");
                        IsRenameMode = true;
                        return;
                    }

                    var ext = System.IO.Path.GetExtension(Path);
                    var destPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), newFileName + ext);
                    DirectoryMonitor.Instance.PauseMonitor();
                    try
                    {
                        File.Move(Path, destPath);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        IsRenameMode = true;
                    }
                    finally
                    {
                        DirectoryMonitor.Instance.ResumeMonitor();
                    }

                    IsRenameMode = false;

                    using (var repos = new RepositoryService())
                    {
                        var movie = repos.ApplyMovie(MovieId, (m => m.Path = destPath));
                        if (movie != null)
                        {
                            logger.Information($"ファイル名を変更しました。{Path} -> {destPath}");
                            Path = destPath;
                        }
                    }
                }));
            }
        }

        private ICommand renameCancelCommand;
        public ICommand RenameCancelCommand
        {
            get
            {
                return renameCancelCommand ?? (renameCancelCommand = new DelegateCommand(() => { IsRenameMode = false; }));
            }
        }

        private ICommand openMovieDirectoryCommand;
        public ICommand OpenMovieDirectoryCommand
        {
            get
            {
                return openMovieDirectoryCommand ?? (openMovieDirectoryCommand = new DelegateCommand(() =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{Path}\"");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }));
            }
        }

        private ICommand updateThumbnailCommand;
        public ICommand UpdateThumbnailCommand
        {
            get
            {
                return updateThumbnailCommand ?? (updateThumbnailCommand = new DelegateCommand<MovieItemViewModel>((model) =>
                {
                    model.IsEnabled = false;

                    using (var repos = new RepositoryService())
                    {
                        var movie = repos.FindMovie(model.MovieId);
                        if (movie != null)
                        {
                            repos.RemoveMovieThumbnails(model.MovieId);
                            var importer = new MovieImporter();
                            importer.UpdateMovieThumbnails(model.Path, Util.MovieThumbnailDirectory, movie);
                            repos.UpdateMovie(movie);
                            OnPropertyChanged("ThumbnailImage");
                        }
                    }

                    model.IsEnabled = true;
                }));
            }
        }
        #endregion
    }
}
