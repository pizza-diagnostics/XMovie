using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models.Repository
{
    public class TagMapRepository : Repository<TagMap>
    {
        public TagMapRepository(System.Data.Entity.DbContext context) : base(context) { }
    }
}
