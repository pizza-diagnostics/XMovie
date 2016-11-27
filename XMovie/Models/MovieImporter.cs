using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XMovie.Common;

namespace XMovie.Models
{
    public class MovieImporter
    {
        private Logger logger = Logger.Instace;

        private bool CreateThumbnailDirectory()
        {
            if (Directory.Exists(Util.MovieThumbnailDirectory))
            {
                return true;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(Util.MovieThumbnailDirectory);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new MovieImporterException("サムネイルディレクトリを作成できません。",
                        ex, MovieImporterException.Error.CannotCreateThumbnailDirectory);
                }
            }
        }

        private double GetMovieDuration(string path)
        {
            using (var process = new Process())
            {
                // TODO: ffprobeのパラメータは固定？
                // TODO: mp3/画像等の扱い -> エラーにならないので、[FORMAT]を全て出力してチェックする?
                var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1 \"{path}\"";
                process.StartInfo = new ProcessStartInfo(Util.FFProbePath, args);
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    throw new MovieImporterException("ffmpeg.exeを実行できません。パスを確認してください。",
                        ex, MovieImporterException.Error.FFProbeNotFound);
                }
                double duration = 0;
                var regex = new Regex(@"^duration=([0-9]*[.]?[0-9]*)");
                string stdout = null;
                while ((stdout = process.StandardOutput.ReadLine()) != null)
                {
                    if (regex.IsMatch(stdout))
                    {
                        duration = Double.Parse(regex.Match(stdout).Groups[1].Captures[0].Value);
                    }
                }
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return duration;
                }
                else
                {
                    throw new MovieImporterException($"{Path.GetFileName(path)}から情報を読み取れません。\n{stderr}",
                        MovieImporterException.Error.FFProbeError);
                }
            }
        }

        public Movie Import(string path)
        {
            CreateThumbnailDirectory();

            Movie movie = null;
            using (var movieContext = new XMovieContext())
            {
                try
                {
                    var md5 = Util.ComputeMD5(path);
                    var info = new FileInfo(path);
                    movie = new Movie()
                    {
                        MovieId = Guid.NewGuid().ToString(),
                        Path = path,
                        MD5Sum = md5,
                        FileCreateDate = info.CreationTime,
                        FileModifiedDate = info.LastWriteTime,
                        RegisteredDate = DateTime.Now,
                    };
                }
                catch (Exception ex)
                {
                    throw new MovieImporterException($"{Path.GetFileName(path)}を開けません。\n",
                        ex, MovieImporterException.Error.CannotReadFile);
                }

                var duration = GetMovieDuration(path);
                logger.Debug($"{path} Duration: {duration}, md5sum: {movie.MD5Sum}");

                // 1ファイルにつき5枚のサムネイルを作成する
                var step = (int)duration / 6; // TODO: サムネイル作成の時間間隔設定方法
                for (var i = 0; i < 5; i++)
                {
                    var seconds = 10 + (i * step);
                    var fileName = $"{movie.MovieId}_{i + 1, 0:D3}.jpg";
                    var thumbnailPath = Path.Combine(Util.MovieThumbnailDirectory, fileName);
                    Thumbnail thumbnail = null;
                    try
                    {
                        thumbnail = CreateMovieThumbnails(path, thumbnailPath, seconds);
                    }
                    catch (Exception ex)
                    {
                        // TODO: サムネイル作成エラーハンドリング
                    }
                    if (thumbnail == null)
                    {
                        break;
                    }
                    else
                    {
                        thumbnail.Movie = movie;
                        thumbnail.MovieId = movie.MovieId;
                        thumbnail.Movie = movie;
                        // 勝手にやるのはOK?
                        if (movie.Thumbnails == null)
                        {
                            movie.Thumbnails = new List<Thumbnail>();
                        }
                        movie.Thumbnails.Add(thumbnail);
                    }
                }
            }
            return movie;
        }

        public Thumbnail CreateMovieThumbnails(string moviePath, string thumbnailPath, int seconds)
        {
            // TODO: 動画以外の扱いをどうするか
            var arg = $"-ss {seconds} -i \"{moviePath}\" -vf scale=160:-1 -f image2 -an -y -vframes 1 \"{thumbnailPath}\"";
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(Util.FFMpegPath);
            process.StartInfo.Arguments = arg;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;

            process.Start();

            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                logger.Information($"サムネイル作成完了: {Path.GetFileName(thumbnailPath)}");
                /*
                if (stderr.Length > 0)
                {
                    logger.Warning(stderr);
                }
                */
                return new Thumbnail()
                {
                    FileName = Path.GetFileName(thumbnailPath),
                    Seconds = seconds
                };
            }
            else
            {
                logger.Warning($"サムネイル作成失敗\r\n{stderr}");
            }

            return null;
        }
    }
}
