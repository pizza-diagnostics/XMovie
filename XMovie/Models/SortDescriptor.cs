using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models
{
    public abstract class SortDescriptor
    {
        public abstract IQueryable<Movie> MovieSort(IQueryable<Movie> query);

        public abstract string GetOrderByString();
    }

    public class SortDescriptor<TKey> : SortDescriptor
    {
        public string Title { get; set; }
        public bool IsAsc { get; set; }
        public string Column { get; set; }

        public Expression<Func<Movie, TKey>> MovieSortFunc { get; set; }

        public override IQueryable<Movie> MovieSort(IQueryable<Movie> query)
        {
            return IsAsc ? query.OrderBy(MovieSortFunc) : query.OrderByDescending(MovieSortFunc);
        }

        public override string GetOrderByString()
        {
            string a = IsAsc ? "asc" : "desc";
            return $" order by {Column} {a} ";
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
