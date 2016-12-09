using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models
{
    public class TagCategory
    {
        public int TagCategoryId { get; set; }

        [Required]
        [Index(IsUnique = true)]
        public string Name { get; set; }

        public int SortOrder { get; set; }
    }
}
