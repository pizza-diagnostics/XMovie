using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.ViewModels
{
    public class TagMapViewModel : ViewModelBase
    {
        public int TagId { get; set; }
        public string Name { get; set; }
        public int TagMapId { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
