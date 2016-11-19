using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.DataModel
{
    public class Thumbnail
    {
        public int ThumbnailId { get; set; }

        [Required]
        public int MovieId { get; set; }

        [Required]
        public string Path { get; set; }
    }
}
