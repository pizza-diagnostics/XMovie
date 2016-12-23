using System.Linq;
using XMovie.Models.Data;

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
