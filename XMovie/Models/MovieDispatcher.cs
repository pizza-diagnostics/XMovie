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
using XMovie.Models.Repository;
using XMovie.ViewModels;

namespace XMovie.Models
{
    public class MovieDispatcher
    {
        private Dispatcher dispatcher;
        private Logger logger = Logger.Instace;

        private volatile bool isShutdown = false;

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
                isShutdown = true;
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            };
        }

        public async Task ImportMovie(string path, ObservableCollection<MovieItemViewModel> movieCollection)
        {
            await dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (App.Current == null)
                    {
                        return;
                    }
                    var importer = new MovieImporter();
                    var movie = importer.Import(path);
                    if (movie == null || isShutdown)
                    {
                        return;
                    }
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            using (var repos = new RepositoryService())
                            {
                                repos.InsertMovie(movie);
                            }
                            movieCollection.Add(new MovieItemViewModel(movie.MovieId));
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }
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
            var moviePath = "";
            await dispatcher.InvokeAsync(() =>
            {
                List<string> thumbnailPaths;
                using (var repos = new RepositoryService())
                {
                    var movie = repos.FindMovie(movieId);
                    if (movie == null)
                    {
                        logger.Information($"動画はすでに登録解除されています。{movieId}");
                        return;
                    }
                    moviePath = movie.Path;
                    thumbnailPaths = repos.FindMovieThumbnails(movieId).Select(t => t.Path).ToList();
                    repos.RemoveMovie(movie);
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
                logger.Information($"削除された動画の登録を解除しました。{moviePath}");
            });

        }

    }
}
