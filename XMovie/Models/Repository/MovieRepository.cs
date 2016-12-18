using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using XMovie.Common;

namespace XMovie.Models.Repository
{
    public class MovieRepository : Repository<Movie>
    {
        public MovieRepository(System.Data.Entity.DbContext context) : base(context) { }

        #region Update
        public Movie Apply(string movieId, Action<Movie> action)
        {
            var movie = Find(movieId);
            if (movie != null)
            {
                action(movie);
                Update(movie);
                return movie;
            }
            return null;
        }
        #endregion

        #region Find

        public int Count()
        {
            return dbSet.Count();
        }

        public Movie FindMovieAtPath(string path)
        {
            var movies = GetAll();
            return  movies.Where(m => Util.IsEqualsNormalizedPath(path, m.Path)).FirstOrDefault();
        }

        public IQueryable<Movie> FindMovies(Expression<Func<Movie, bool>> predicate, SortDescriptor sort)
        {
            return sort.MovieSort(dbSet.Where(predicate));
        }

        public IQueryable<Movie> FindDuplicateMovies(SortDescriptor sort)
        {
            var md5list = from m in dbSet
                          group m by m.MD5Sum into grouped
                          where grouped.Count() > 1
                          select grouped.Key;

            return sort.MovieSort(dbSet.Where(m => md5list.Contains(m.MD5Sum)));
        }

        public IQueryable<Movie> FindMoviesByPathKeys(List<string> pathKeys, SortDescriptor sort)
        {
            //context.Database.Log = s => System.Diagnostics.Debug.Print(s);

            // NOTE: Path.ContainsがCHARINDEXになり日本語が正しく検索できないため、queryを記述する

            var q = "select * from Movies";
            if (pathKeys.Count() > 0)
            {
                q += " where " + String.Join(" and ", pathKeys.Select((k, i) => $"Path like @p{i}"));
            }

            q += sort.GetOrderByString();

            return context.Database.SqlQuery<Movie>(q, pathKeys.Select(k => $"%{k}%").ToArray()).AsQueryable<Movie>();
        }

        #endregion
    }
}
