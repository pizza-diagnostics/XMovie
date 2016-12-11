using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models.Settings
{
    public class FileExtensionSettings : ICloneable
    {
        public string Ext { get; set; }
        public bool IsEnabled { get; set; } = true;

        public static ObservableCollection<FileExtensionSettings> GetDefaultMovieExtensions()
        {
            var list = new string[]
            {
                "avi",
                "flv",
                "m4v",
                "mov",
                "mp4",
                "mpeg",
                "mpg",
                "rm",
                "wmv",
                "ts",
            };

            var result = new ObservableCollection<FileExtensionSettings>();
            foreach (var ext in list)
            {
                result.Add(new FileExtensionSettings() { Ext = ext, IsEnabled = true });
            }
            return result;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
