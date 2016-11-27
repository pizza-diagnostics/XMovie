using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Common;

namespace XMovie.Models
{
    public class Thumbnail
    {
        public int ThumbnailId { get; set; }

        [Required]
        public string MovieId { get; set; }

        [Required]
        public string FileName { get; set; }

        public int Seconds { get; set; }

        [ForeignKey("MovieId")]
        public virtual Movie Movie { get; set; }

        public string Path
        {
            get { return System.IO.Path.Combine(Util.MovieThumbnailDirectory, FileName); }
        }
    }
}
