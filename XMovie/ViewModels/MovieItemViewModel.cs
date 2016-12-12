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
using XMovie.Models.Settings;

namespace XMovie.ViewModels
{
    public class MovieItemViewModel : ViewModelBase
    {
        private Logger logger = Logger.Instace;

        public MovieItemViewModel()
        {
            ThumbnailCount = UserSettingManager.Instance.GetUserSettings().ThumbnailCount;
        }

        public MovieItemViewModel(string movieId) : this()
        {
            MovieId = movieId;
            using (var context = new XMovieContext())
            {
                var movie = context.Movies.Find(MovieId);
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

        private ObservableCollection<TagMapViewModel> tagMaps;
        public ObservableCollection<TagMapViewModel> TagMaps
        {
            get { return this.tagMaps; }
            set { SetProperty(ref tagMaps, value, "TagMaps"); }
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
                if (value < 1 || value > 5)
                {
                    SetError("thumbnailCount", "サムネイル数は1から5に設定する必要があります。");
                }
                else
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
                    using (var context = new XMovieContext())
                    {
                        var thumbnails = context.Thumbnails
                                                .Where(t => t.MovieId.Equals(MovieId))
                                                .OrderBy(t => t.Seconds)
                                                .ToArray();
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
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    return null;
                }
            }
        }

        public void UpdateTags()
        {
            using (var context = new XMovieContext())
            {
                var tags = from tm in context.TagMaps
                           join t in context.Tags
                           on tm.TagId equals t.TagId
                           where tm.MovieId == MovieId
                           select new TagMapViewModel(){ TagId = tm.TagId, TagMapId = tm.TagMapId, Name = t.Name };

                TagMaps = new ObservableCollection<TagMapViewModel>(tags);
            }
        }

        #region Command
        private RelayCommand changeRankCommand;
        public ICommand ChangeRankCommand
        {
            get
            {
                if (changeRankCommand == null)
                {
                    changeRankCommand = new RelayCommand((param) =>
                    {
                        using (var context = new XMovieContext())
                        {
                            var movie = context.Movies.Find(MovieId);
                            movie.Rank += (int)param;
                            Rank = movie.Rank;
                            context.SaveChanges();
                        }
                        OnPropertyChanged("Movie.Rank");
                    });
                }
                return changeRankCommand;

            }
        }

        private RelayCommand playMovieCommand;
        public ICommand PlayMovieCommand
        {
            get
            {
                if (playMovieCommand == null)
                {
                    playMovieCommand = new RelayCommand(param =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(Path);
                            using (var context = new XMovieContext())
                            {
                                var movie = context.Movies.Find(MovieId);
                                PlayCount = ++movie.PlayCount;
                                context.SaveChanges();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }
                    });
                }
                return playMovieCommand;
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
                        var tagParam = (Tag)param;
                        var isExist = TagMaps.Where(tm => tm.TagId == tagParam.TagId).Count() > 0;
                        if (!isExist)
                        {
                            // TODO: 冗長
                            UpdateTags();
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
                        var remove = TagMaps.Where(tm => tm.TagId == ((Tag)param).TagId).FirstOrDefault();
                        TagMaps.Remove(remove);
                    });
                }
                return removeTagCommand;
            }
        }

        private ICommand beginRenameCommand;
        public ICommand BeginRenameCommand
        {
            get
            {
                if (beginRenameCommand == null)
                {
                    beginRenameCommand = new RelayCommand((param) => { IsRenameMode = true; });
                }
                return beginRenameCommand;
            }
        }

        private ICommand renameCommand;
        public ICommand RenameCommand
        {
            get
            {
                if (renameCommand == null)
                {
                    renameCommand = new RelayCommand((param) =>
                    {
                        string newFileName = (string)param;
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

                        using (var context = new XMovieContext())
                        {
                            var movie = context.Movies.Where(m => m.MovieId == MovieId).FirstOrDefault();
                            if (movie != null)
                            {
                                logger.Information($"ファイル名を変更しました。{movie.Path} -> {destPath}");
                                movie.Path = destPath;
                            }
                            context.SaveChanges();
                            Path = destPath;
                        }
                    });
                }
                return renameCommand;
            }
        }

        private ICommand renameCancelCommand;
        public ICommand RenameCancelCommand
        {
            get
            {
                if (renameCancelCommand == null)
                {
                    renameCancelCommand = new RelayCommand((param) =>
                    {
                        IsRenameMode = false;
                    });
                }
                return renameCancelCommand;
            }
        }

        private ICommand openMovieDirectoryCommand;
        public ICommand OpenMovieDirectoryCommand
        {
            get
            {
                if (openMovieDirectoryCommand == null)
                {
                    openMovieDirectoryCommand = new RelayCommand((param) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{Path}\"");
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }
                    });
                }
                return openMovieDirectoryCommand;
            }
        }

        private ICommand updateThumbnailCommand;
        public ICommand UpdateThumbnailCommand
        {
            get
            {
                if (updateThumbnailCommand == null)
                {
                    updateThumbnailCommand = new RelayCommand((param) =>
                    {
                        var model = (MovieItemViewModel)param;
                        model.IsEnabled = false;
                        using (var context = new XMovieContext())
                        {
                            var movie = context.Movies.Where(m => m.MovieId == model.MovieId).FirstOrDefault();
                            if (movie != null)
                            {
                                var thumbnails = context.Thumbnails.Where(t => t.MovieId == model.MovieId);
                                context.Thumbnails.RemoveRange(thumbnails);
                                movie.Thumbnails.Clear();

                                var importer = new MovieImporter();
                                importer.UpdateMovieThumbnails(movie.Path, Util.MovieThumbnailDirectory, movie);
                                OnPropertyChanged("ThumbnailImage");
                            }
                            context.SaveChanges();
                        }
                        model.IsEnabled = true;
                    });
                }
                return updateThumbnailCommand;
            }
        }
        #endregion

    }
}
