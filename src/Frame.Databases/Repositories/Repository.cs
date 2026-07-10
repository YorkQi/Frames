using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Entities;
using Frame.Core.Repositories;

namespace Frame.Databases.Repositories
{
    /// <summary>
    /// 泛型实体仓储基类。通过 <see cref="DbContextAccessor"/> 获取 IDbContext，
    /// 子类无需通过构造函数传递，可直接访问 DBContext 执行 CRUD 和原始 SQL。
    /// </summary>
    /// <typeparam name="TPrimaryKey">主键类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class Repository<TPrimaryKey, TEntity>
        : IRepository<TPrimaryKey, TEntity> where TEntity : class, IEntity<TPrimaryKey>
    {
        private IDbContext? _dbContext;

        /// <summary>
        /// 获取当前作用域的 <see cref="IDbContext"/>，首次访问时缓存。
        /// </summary>
        protected IDbContext DBContext => _dbContext ??= DbContextAccessor.Current;

        public TEntity Get(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            return DBContext.Get<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);
        }
        public Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            return DBContext.GetAsync<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);
        }
        public IEnumerable<TEntity> Query(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
        {
            return DBContext.GetEntity<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);
        }
        public Task<IEnumerable<TEntity>> QueryAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
        {
            return DBContext.GetEntityAsync<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);
        }
        public IEnumerable<TEntity> QueryAll(CancellationToken cancellationToken = default)
        {
            return DBContext.GetAllEntity<TPrimaryKey, TEntity>(cancellationToken: cancellationToken);
        }
        public Task<IEnumerable<TEntity>> QueryAllAsync(CancellationToken cancellationToken = default)
        {
            return DBContext.GetAllEntityAsync<TPrimaryKey, TEntity>(cancellationToken: cancellationToken);
        }
        public int Insert(TEntity entity, CancellationToken cancellationToken = default)
        {
            return DBContext.Insert<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);
        }
        public Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return DBContext.InsertAsync<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);
        }
        public int InsertBatch(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default)
        {
            return DBContext.InsertBatch<TPrimaryKey, TEntity>(entitys, cancellationToken: cancellationToken);
        }
        public Task<int> InsertBatchAsync(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default)
        {
            return DBContext.InsertBatchAsync<TPrimaryKey, TEntity>(entitys, cancellationToken: cancellationToken);
        }
        public int Update(TEntity entity, CancellationToken cancellationToken = default)
        {
            return DBContext.Update<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);
        }
        public Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return DBContext.UpdateAsync<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);
        }
        public int UpdateBatch(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default)
        {
            return DBContext.UpdateBatch<TPrimaryKey, TEntity>(entitys, cancellationToken: cancellationToken);
        }
        public Task<int> UpdateBatchAsync(IEnumerable<TEntity> entitys, CancellationToken cancellationToken = default)
        {
            return DBContext.UpdateBatchAsync<TPrimaryKey, TEntity>(entitys, cancellationToken: cancellationToken);
        }
        public int Delete(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            return DBContext.Delete<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);
        }
        public Task<int> DeleteAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            return DBContext.DeleteAsync<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);
        }
        public int DeleteBatch(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
        {
            return DBContext.DeleteBatch<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);
        }
        public Task<int> DeleteBatchAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
        {
            return DBContext.DeleteBatchAsync<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);
        }

        #region IDisposable / IAsyncDisposable

        /// <summary>
        /// 释放资源。因 <see cref="IDbContext"/> 由外部作用域管理，
        /// 此处仅清除本地缓存引用，不负责释放 DBContext 本身。
        /// </summary>
        public void Dispose()
        {
            _dbContext = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 异步释放资源。
        /// </summary>
        public ValueTask DisposeAsync()
        {
            _dbContext = null;
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
