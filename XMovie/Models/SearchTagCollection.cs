using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Models.Repository;

namespace XMovie.Models
{
    public class SearchTagCollection
    {
        public string CategoryName { get; set; }

        public List<string> Tags { get; set; }

        public override string ToString()
        {
            return CategoryName;
        }

        public void SetCategory(TagCategory category)
        {
            using (var repos = new RepositoryService())
            {
                Tags = repos.FindCategoryTags(category.TagCategoryId).Select(t => t.Name).ToList();
            }
            CategoryName = category.Name;
        }
    }
}
