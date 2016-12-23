using XMovie.Models.Data;

namespace XMovie.Models.Repository
{
    public class TagMapRepository : Repository<TagMap>
    {
        public TagMapRepository(System.Data.Entity.DbContext context) : base(context) { }
    }
}
