using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models.Repository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        TEntity Find(params object[] keys);
        IList<TEntity> FindAll(Expression<Func<TEntity, bool>> match);
        IList<TEntity> GetAll();

        void Insert(TEntity entity);
        void Delete(TEntity entity);
        void Update(TEntity entity);
    }
}
