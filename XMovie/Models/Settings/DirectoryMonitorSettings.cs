using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models.Settings
{
    public class DirectoryMonitorSettings : ICloneable
    {
        public string Path { get; set; }

        public bool IsMonitorEnabled { get; set; } = true;

        public bool IsBootCheckEnabled { get; set; } = true;

        public bool IsRecursive { get; set; } = true;

        public object Clone()
        {
            return MemberwiseClone();
        }

    }
}
