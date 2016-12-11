using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using XMovie.Common;
using XMovie.ViewModels;

namespace XMovie.Models
{
    public class MovieDispatcher
    {
        private Dispatcher dispatcher;
        private Logger logger = Logger.Instace;

        public MovieDispatcher()
        {
            var source = new TaskCompletionSource<Dispatcher>();
            var thread = new Thread(new ThreadStart(() =>
            {
                source.SetResult(Dispatcher.CurrentDispatcher);
                Dispatcher.Run();
            }));

            thread.Start();
            dispatcher = source.Task.Result;

            Dispatcher.CurrentDispatcher.ShutdownStarted += (s, e) =>
            {
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            };
        }

        public async Task ImportMovie(string path, ObservableCollection<MovieItemViewModel> movieCollection)
        {
            await dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var importer = new MovieImporter();
                    var movie = importer.Import(path);
                    if (movie == null)
                    {
                        return;
                    }
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
                        movieCollection.Add(new MovieItemViewModel(movie.MovieId));
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


            });
        }

        public async Task UnregisterMovie(string movieId, ObservableCollection<MovieItemViewModel> movieCollection)
        {
            await dispatcher.InvokeAsync(() =>
            {
                List<string> thumbnailPaths;
                using (var context = new XMovieContext())
                {
                    var tags = context.TagMaps.Where(tm => tm.MovieId == movieId).ToList();
                    context.TagMaps.RemoveRange(tags);
                    var thumbs = context.Thumbnails.Where(t => t.MovieId == movieId).ToList();
                    thumbnailPaths = thumbs.Select(t => t.Path).ToList();
                    context.Thumbnails.RemoveRange(thumbs);

                    var mov = context.Movies.Where(m => m.MovieId == movieId).FirstOrDefault();
                    // (サブディレクトリとして)同じディレクトリが登録されている場合、
                    // 削除イベントが重複する
                    if (mov != null)
                    {
                        context.Movies.Remove(mov);
                        context.SaveChanges();
                    }
                }

                var model = movieCollection.Where(m => m.MovieId == movieId).FirstOrDefault();
                if (model != null)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        movieCollection.Remove(model);
                    });

                    foreach (var path in thumbnailPaths)
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }
                        }
                        catch (Exception)
                        {
                            logger.Warning($"サムネイル{path}が削除できませんでした。");
                        }
                    }
                }
            });

        }

        /*
        public async Task RemoveMovie(string path, ObservableCollection<MovieItemViewModel> movieCollection)
        {
            await dispatcher.InvokeAsync(() =>
            {
                App.Current.Dispatcher.Invoke(async () =>
                {
                    ICollection<string> movieIds;
                    using (var context = new XMovieContext())
                    {
                        var keys = context.Movies.Select(m => new { m.Path, m.MovieId });
                        movieIds = keys.Where(tmp => Util.IsEqualsNormalizedPath(tmp.Path, path))
                                       .Select(tmp => tmp.MovieId)
                                       .ToList();
                    }
                    foreach (var m in movieIds)
                    {
                        await UnregisterMovie(m, movieCollection);
                    }
                });

            });
        }
        */
    }
}
