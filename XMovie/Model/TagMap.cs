using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Model
{
    public class TagMap
    {
        public int TagMapId { get; set; }

        [Required]
        [Index("IX_TagMovie", 1, IsUnique = true)]
        public int TagId { get; set; }

        [Required]
        [Index("IX_TagMovie", 2, IsUnique = true)]
        public int MovieId { get; set; }
    }
}
