using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Common;
using XMovie.Models.Settings;

namespace XMovie.Models
{
    public delegate void MovieNameChangedDelegate(string oldPath, string path);
    public delegate void MovieAddedDelegate(string path);
    public delegate void MovieRemovedDelegate(string path);

    public sealed class DirectoryMonitor
    {
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        private List<string> targetExt;

        private Logger logger = Logger.Instace;

        public event MovieNameChangedDelegate MovieNameChanged;
        public event MovieAddedDelegate MovieAdded;
        public event MovieRemovedDelegate MovieRemoved;

        private static readonly Lazy<DirectoryMonitor> instance = new Lazy<DirectoryMonitor>(() => new DirectoryMonitor());

        public static DirectoryMonitor Instance
        {
            get { return instance.Value; }
        }

        private DirectoryMonitor() { }

        public void StartMonitor(ICollection<DirectoryMonitorSettings> directories, List<string> extensions)
        {
            StopMonitor();
            targetExt = extensions;

            foreach (var directory in directories)
            {
                if (!directory.IsMonitorEnabled)
                {
                    continue;
                }
                if (!Directory.Exists(directory.Path))
                {
                    logger.Warning($"監視対象ディレクトリ{directory.Path}が見つかりません。");
                    continue;
                }
                var watcher = new FileSystemWatcher();
                watcher.Path = directory.Path;
                watcher.IncludeSubdirectories = directory.IsRecursive;
                watcher.Filter = "*.*";
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;

                watcher.Deleted += Watcher_Deleted;
                watcher.Renamed += Watcher_Renamed;
                watcher.Created += Watcher_Created;
//                watcher.Changed += Watcher_Changed;

                logger.Information($"{directory.Path}の監視を開始します。");

                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }
        }

        public void StopMonitor()
        {
            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Deleted -= Watcher_Deleted;
                watcher.Renamed -= Watcher_Renamed;
                watcher.Created -= Watcher_Created;
                watcher.Dispose();
            }
            watchers.Clear();
        }

        public void PauseMonitor()
        {
            foreach (var watchar in watchers)
            {
                watchar.EnableRaisingEvents = false;
            }
        }

        public void ResumeMonitor()
        {
            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        private bool IsTargetFile(string path)
        {
            if (File.Exists(path))
            {
                var ext = Path.GetExtension(path).ToLower();
                if (targetExt.Contains(ext))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsFileLocked(string path)
        {
            if (File.Exists(path))
            {
                FileStream stream = null;
                try
                {
                    stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (Exception)
                {
                    return true;
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }
            return false;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            logger.Information($"ファイルが更新されました。{e.FullPath}");
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (IsTargetFile(e.FullPath))
            {
                logger.Information($"新しいファイルが見つかりました。{e.FullPath}");
                Task.Run(async () =>
                {
                    while (true)
                    {
                        logger.Debug("ファイルアクセス許可待機中...");
                        await Task.Delay(500);
                        if (!IsFileLocked(e.FullPath))
                        {
                            if (File.Exists(e.FullPath))
                            {
                                logger.Information($"ファイルが追加されました。{e.FullPath}");
                                MovieAdded?.Invoke(e.FullPath);
                            }
                            else
                            {
                                logger.Information($"追加ファイルがなくなりました。{e.FullPath}");
                            }
                            break;
                        }
                    }
                });
            }
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            var created = IsTargetFile(e.FullPath);
            var removed = false;
            // NOTE: 既に削除されているのでFile.Existsは通らない
            var ext = Path.GetExtension(e.OldName).ToLower();
            if (targetExt.Contains(ext))
            {
                removed = true;
            }

            if (created && removed)
            {
                // 変更前後共に対応している拡張子
                logger.Information($"ファイル名が変更されました。{e.OldFullPath} -> {e.FullPath}");
                MovieNameChanged?.Invoke(e.OldFullPath, e.FullPath);
            }
            else if (created)
            {
                // 変更後のみが対応している拡張子(追加)
                logger.Information($"ファイル名が変更され、登録対象になりました。{e.FullPath}");
                MovieAdded?.Invoke(e.FullPath);
            }
            else if (removed)
            {
                // 変更前のみが対応している拡張子(削除)
                // 削除はしない
                logger.Information($"ファイル名が変更されました。{e.OldFullPath} -> {e.FullPath}");
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            // NOTE: 既に削除されているのでFile.Existsは通らない
            var ext = Path.GetExtension(e.Name).ToLower();
            if (targetExt.Contains(ext))
            {
                logger.Information($"ファイルが削除されました。{e.FullPath}");
                MovieRemoved?.Invoke(e.FullPath);
            }
        }

    }
}
