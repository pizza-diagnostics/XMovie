using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Model;

namespace XMovie.ViewModel
{
    public class MovieListViewModel : ViewModelBase
    {
        public ObservableCollection<Movie> Movies { get; private set; }


    }
}
