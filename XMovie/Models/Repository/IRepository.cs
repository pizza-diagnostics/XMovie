using System;
using System.Linq;
using System.Linq.Expressions;

namespace XMovie.Models.Repository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        TEntity Find(params object[] keys);

        IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> match);
        IQueryable<TEntity> GetAll();

        void Insert(TEntity entity);
        void Delete(TEntity entity);
        void Update(TEntity entity);
    }
}
