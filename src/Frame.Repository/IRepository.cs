using Frame.Core.FrameModules;
using Frame.Repository.DBContexts;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frame.Repository
{
    [FrameModule(ServiceLifetime.Singleton)]
    public interface IRepository
    {
        void Initialize<TDbContext>(TDbContext dbContext) where TDbContext : IDBContext;

    }

    /// <summary>
    /// 定义仓储必须指向实体和实体主键类型
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepository<TPrimaryKey, TEntity> : IRepository
    {
        Task<TEntity> GetAsync(TPrimaryKey id);
        Task<IEnumerable<TEntity>> QueryAllAsync();
        Task<IEnumerable<TEntity>> QueryAsync(IEnumerable<TPrimaryKey> ids);
        Task<int> InsertAsync(TEntity entity);
        Task<int> InsertBatchAsync(IEnumerable<TEntity> entitys);
        Task<int> UpdateAsync(TEntity entity);
        Task<int> UpdateBatchAsync(IEnumerable<TEntity> entitys);
        Task<int> DeleteAsync(TPrimaryKey id);
        Task<int> DeleteBatchAsync(IEnumerable<TPrimaryKey> ids);
    }
}
