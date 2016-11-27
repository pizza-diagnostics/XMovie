using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.ViewModels
{
    public class UserSettingsViewModel : ViewModelBase
    {
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
                    }
                }
            }
        }
    }
}
