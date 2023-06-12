using Frame.Core.Entitys;
using Frame.Repository.Context;
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
        public IDBContext? Context { get; set; }

        public Task<TEntity> GetAsync(TPrimaryKey id)
        {
            return Context.Get<TEntity>(id ?? new object());
        }
        public Task<IEnumerable<TEntity>> QueryAsync(IEnumerable<TPrimaryKey> ids)
        {
            return Context.QueryEntity<TEntity>(ids);
        }

        public Task<int> InsertAsync(TEntity entity)
        {
            return Context.Insert(entity);
        }

        public Task<int> InsertBatchAsync(IEnumerable<TEntity> entitys)
        {
            return Context.InsertBatch(entitys);
        }

        public Task<int> UpdateAsync(TEntity entity)
        {
            return Context.Update(entity);
        }

        public Task<int> UpdateBatchAsync(IEnumerable<TEntity> entitys)
        {
            return Context.UpdateBatch(entitys);
        }

        public Task<int> DeleteAsync(TPrimaryKey id)
        {
            return Context.Delete<TEntity>(id ?? new object());
        }

        public Task<int> DeleteBatchAsync(IEnumerable<TPrimaryKey> ids)
        {
            return Context.DeleteBatch<TEntity>(ids);
        }
    }
}
