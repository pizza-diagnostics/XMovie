using System.Linq;
using XMovie.Models.Data;

namespace XMovie.Models.Repository
{
    public class TagCategoryRepository : Repository<TagCategory>
    {
        public TagCategoryRepository(System.Data.Entity.DbContext context) : base(context) { }

        public bool IsExistTagCategory(string name)
        {
            return dbSet.Any(c => c.Name.Equals(name));
        }

        public TagCategory InsertNewCategory(string name)
        {
            TagCategory category = dbSet.Where(c => c.Name.Equals(name)).FirstOrDefault();
            if (category == null)
            {
                category = new TagCategory()
                {
                    Name = name,
                };
                Insert(category);
            }
            return category;
        }
    }
}
