using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models.Repository
{
    public class ThumbnailRepository : Repository<Thumbnail>
    {
        public ThumbnailRepository(System.Data.Entity.DbContext context) : base(context) { }

        public IQueryable<Thumbnail> FindMovieThumbnails(string movieId)
        {
            return dbSet.Where(t => t.MovieId.Equals(movieId)).OrderBy(t => t.Seconds);
        }
    }
}
