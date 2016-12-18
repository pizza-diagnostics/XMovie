using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models.Repository
{
    public class Repository<TEntity> : IRepository<TEntity>
        where TEntity : class
    {

        protected DbContext context;
        protected DbSet<TEntity> dbSet;

        protected Repository(DbContext context)
        {
            this.context = context;
            dbSet = context.Set<TEntity>();
        }

        public TEntity Find(params object[] keys)
        {
            return dbSet.Find(keys);
        }

        public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> predicate)
        {
            return dbSet.Where(predicate);
        }

        public IQueryable<TEntity> GetAll()
        {
            return dbSet;
        }

        public void Delete(TEntity entity)
        {
            dbSet.Remove(entity);
            context.SaveChanges();
        }

        public void DeleteRange(ICollection<TEntity> list)
        {
            dbSet.RemoveRange(list);
            context.SaveChanges();
        }


        public void Insert(TEntity entity)
        {
            dbSet.Add(entity);
            context.SaveChanges();
        }

        public void Update(TEntity entity)
        {
            dbSet.Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
            context.SaveChanges();
        }
    }
}
