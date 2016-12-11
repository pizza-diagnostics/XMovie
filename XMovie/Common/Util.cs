using System;
using System.IO;
using System.Security.Cryptography;

namespace XMovie.Common
{
    public class Util
    {
        public static string ApplicationDirectory
        {
            get
            {
                string assemblyPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                return Path.GetDirectoryName(assemblyPath);
            }
        }

        public static string MovieThumbnailDirectory
        {
            get
            {
                return Path.Combine(ApplicationDirectory, "thumbnails");
            }
        }

        public static string UserSettingFilePath
        {
            get
            {
                return Path.Combine(ApplicationDirectory, "xmovie.json");
            }
        }

        public static string FFProbePath
        {
            get
            {
                return Path.Combine(ApplicationDirectory, "ffprobe.exe");
            }
        }

        public static string FFMpegPath
        {
            get
            {
                return Path.Combine(ApplicationDirectory, "ffmpeg.exe");
            }
        }


        public static string ComputeMD5(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                return BitConverter.ToString(MD5.Create().ComputeHash(stream)).ToLower().Replace("-", "");
            }
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        public static bool IsEqualsNormalizedPath(string path1, string path2)
        {
            return NormalizePath(path1).Equals(NormalizePath(path2));
        }
    }
}
