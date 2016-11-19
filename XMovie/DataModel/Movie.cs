using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.DataModel
{
    public class Movie
    {
        public int MovieId { get; set; }

        [Required]
        public string Path { get; set; }

        public int Rank { get; set; }
        public int PlayCount { get; set; }

        public DateTime FileCreateDate { get; set; }

        public DateTime FileModifiedDate { get; set; }

        public DateTime RegsiteredDate { get; set; }
    }
}
