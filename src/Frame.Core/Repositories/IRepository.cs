using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Repositories
{
    /// <summary>
    /// 仓储标记接口。具体仓储通过自动注册。
    /// </summary>
    public interface IRepository : IDisposable, IAsyncDisposable
    {
    }

    /// <summary>
    /// 定义仓储必须指向实体和实体主键类型
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepository<TPrimaryKey, TEntity> : IRepository
    {
        TEntity Get(TPrimaryKey id, CancellationToken cancellationToken = default);
        Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default);
        IEnumerable<TEntity> QueryAll(CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> QueryAllAsync(CancellationToken cancellationToken = default);
        IEnumerable<TEntity> Query(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> QueryAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);
        int Insert(TEntity entity, CancellationToken cancellationToken = default);
        Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
        int InsertBatch(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default);
        Task<int> InsertBatchAsync(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default);
        int Update(TEntity entity, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        int UpdateBatch(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default);
        Task<int> UpdateBatchAsync(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default);
        int Delete(TPrimaryKey id, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(TPrimaryKey id, CancellationToken cancellationToken = default);
        int DeleteBatch(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);
        Task<int> DeleteBatchAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);
    }
}
