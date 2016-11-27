using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
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
        public MovieItemViewModel()
        {
            ThumbnailCount = UserSettingManager.Instance.GetUserSettings().ThumbnailCount;
        }

        public MovieItemViewModel(string movieId) : this()
        {
            this.movieId = movieId;
            using (var context = new XMovieContext())
            {
                var movie = context.Movies.Find(movieId);
                Rank = movie.Rank;
                PlayCount = movie.PlayCount;
                Path = movie.Path;
            }
        }

        private string movieId;
        private Logger logger = Logger.Instace;

        private int rank;
        public int Rank
        {
            get { return rank; }
            set
            {
                SetProperty(ref rank, value, "Rank");
            }
        }

        private int playCount;
        public int PlayCount
        {
            get { return playCount; }
            set
            {
                SetProperty(ref playCount, value, "PlayCount");
            }
        }

        private string path;
        public string Path
        {
            get { return path; }
            set
            {
                SetProperty(ref path, value, "Path");
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
                using (var context = new XMovieContext())
                {
                    var thumbnails = context.Thumbnails.Where(t => t.MovieId.Equals(movieId)).ToArray();
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
                            var bitmapImage = new BitmapImage(new Uri(item.t.Path));
                            dc.DrawImage(bitmapImage, new Rect(x, 0, bitmapImage.PixelWidth, bitmapImage.PixelHeight));
                            y = bitmapImage.PixelHeight; // 固定
                            x += bitmapImage.PixelWidth;
                        }
                    }
                    var bitmap = new RenderTargetBitmap(x, y, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(visual);

                    return bitmap;
                }
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
                            var movie = context.Movies.Find(movieId);
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
                                var movie = context.Movies.Find(movieId);
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
        #endregion

    }
}
