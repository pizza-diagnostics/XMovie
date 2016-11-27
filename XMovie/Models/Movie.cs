using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models
{
    public class Movie
    {
        public Movie()
        {
            Thumbnails = new ObservableCollection<Thumbnail>();
        }

        public string MovieId { get; set; }

        [Required]
        public string Path { get; set; }

        public int Rank { get; set; }
        public int PlayCount { get; set; }

        public string MD5Sum { get; set; }

        public DateTime FileCreateDate { get; set; }
        public DateTime FileModifiedDate { get; set; }
        public DateTime RegisteredDate { get; set; }

        public virtual ICollection<Thumbnail> Thumbnails { get; set; }

    }
}
