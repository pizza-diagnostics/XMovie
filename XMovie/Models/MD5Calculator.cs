using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XMovie.Common;
using XMovie.Models.Repository;

namespace XMovie.Models
{
    public class MD5Calculator
    {
        private SemaphoreSlim semaphore;

        private Logger logger;

        private bool isShutdown = false;

        public MD5Calculator()
        {
            semaphore = new SemaphoreSlim(1);
            logger = App.Container.Resolve<Logger>();
        }

        public async Task Shutdown()
        {
            await semaphore.WaitAsync();
            logger.Debug("MD5: 終了要求受信");
            isShutdown = true;
            semaphore.Release();
        }

        public void Request(string movieId)
        {
            if (isShutdown)
            {
                return;
            }
            Task.Run(async () =>
            {
                logger.Debug($"MD5: MD5計算リクエスト受付: {movieId}");
                await semaphore.WaitAsync();

                try
                {
                    string path = null;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        using (var repos = new RepositoryService())
                        {
                            var movie = repos.FindMovie(movieId);
                            if (movie != null)
                            {
                                path = movie.Path;
                            }
                        }
                    });

                    if (path == null)
                    {
                        logger.Debug($"MD5: 動画ファイルが見つかりません。{path}");
                    }
                    else
                    {
                        logger.Debug($"MD5: 計算中... {path}");
                        var md5 = Util.ComputeMD5(path);
                        logger.Debug($"MD5: {md5}  {path}");

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            using (var repos = new RepositoryService())
                            {
                                var movie = repos.FindMovie(movieId);
                                if (movie == null)
                                {
                                    logger.Debug($"MD5: 動画は削除されています。{path}");
                                }
                                else
                                {
                                    movie.MD5Sum = md5;
                                    repos.UpdateMovie(movie);
                                    logger.Debug($"MD5: 動画のMD5を設定しました。 {path}");
                                }
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }
    }
}
