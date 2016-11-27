using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models
{
    public class Tag
    {
        public int TagId { get; set; }

        [Required]
        [Index("IX_TagName", 1, IsUnique = true)]
        public int TagCategoryId { get; set; }

        [Required]
        [Index("IX_TagName", 2, IsUnique = true)]
        public string Name { get; set; }
    }
}
