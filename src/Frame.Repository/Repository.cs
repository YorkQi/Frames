using Frame.Repository.DBContexts;
using Frame.Repository.Entitys;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frame.Repository
{
    /// <summary>
    /// 用于继承后构建的实体仓储
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public class Repository<TPrimaryKey, TEntity>
        : IRepository<TPrimaryKey, TEntity> where TEntity : IEntity
    {
        public IDBContext DBContext { get; set; } = default!;
        public void Initialize<TDbContext>(TDbContext dbContext) where TDbContext : IDBContext
        {
            DBContext = dbContext;
        }

        public Task<TEntity> GetAsync(TPrimaryKey id)
        {
            return DBContext.Get<TEntity>(id ?? new object());
        }
        public Task<IEnumerable<TEntity>> QueryAsync(IEnumerable<TPrimaryKey> ids)
        {
            return DBContext.QueryEntity<TEntity>(ids);
        }
        public Task<IEnumerable<TEntity>> QueryAllAsync()
        {
            return DBContext.QueryAllEntity<TEntity>();
        }
        public Task<int> InsertAsync(TEntity entity)
        {
            return DBContext.Insert(entity);
        }
        public Task<int> InsertBatchAsync(IEnumerable<TEntity> entitys)
        {
            return DBContext.InsertBatch(entitys);
        }
        public Task<int> UpdateAsync(TEntity entity)
        {
            return DBContext.Update(entity);
        }
        public Task<int> UpdateBatchAsync(IEnumerable<TEntity> entitys)
        {
            return DBContext.UpdateBatch(entitys);
        }
        public Task<int> DeleteAsync(TPrimaryKey id)
        {
            return DBContext.Delete<TEntity>(id ?? new object());
        }
        public Task<int> DeleteBatchAsync(IEnumerable<TPrimaryKey> ids)
        {
            return DBContext.DeleteBatch<TEntity>(ids);
        }
    }
}
