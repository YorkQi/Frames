using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Entities;
using Frame.Core.Repositories;

namespace Frame.Databases.Repositories
{
    /// <summary>
    /// 泛型实体仓储基类。
    /// </summary>
    /// <typeparam name="TPrimaryKey">主键类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class Repository<TPrimaryKey, TEntity>
        : IRepository<TPrimaryKey, TEntity> where TEntity : class, IEntity<TPrimaryKey>
    {
        private IDbContext? _dbContext;

        /// <summary>
        /// 数据库上下文。
        /// </summary>
        protected IDbContext DBContext
        {
            get
            {
                // 已通过构造函数显式注入 → 直接返回
                if (_dbContext is not null)
                    return _dbContext;

                // 尝试从当前请求 Scope 延迟解析
                var ambient = DbContextScope.Current;
                if (ambient is not null)
                {
                    _dbContext = ambient;
                    return _dbContext;
                }

                throw new System.InvalidOperationException(
                    "IDbContext 不可用。请确保通过 DatabaseContext 获取仓储实例，" +
                    "或在构造函数中显式注入 IDbContext。");
            }
        }

        /// <summary>
        /// 无参构造函数（推荐）。
        protected Repository()
        {
        }

        /// <summary>
        /// 构造函数注入 IDbContext。
        /// DI 容器在解析仓储时会自动注入与当前 Scope 关联的 IDbContext 实例。
        /// 当使用此构造函数时，<see cref="DBContext"/> 直接使用注入值，不经过延迟解析。
        /// </summary>
        /// <param name="dbContext">数据库上下文接口</param>
        public Repository(IDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new System.ArgumentNullException(nameof(dbContext));
        }



        public TEntity Get(TPrimaryKey id, CancellationToken cancellationToken = default)
            => DBContext.Get<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);

        public Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
            => DBContext.GetAsync<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);

        public IEnumerable<TEntity> QueryAll(CancellationToken cancellationToken = default)
            => DBContext.GetAllEntity<TPrimaryKey, TEntity>(cancellationToken: cancellationToken);

        public Task<IEnumerable<TEntity>> QueryAllAsync(CancellationToken cancellationToken = default)
            => DBContext.GetAllEntityAsync<TPrimaryKey, TEntity>(cancellationToken: cancellationToken);

        public IEnumerable<TEntity> Query(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
            => DBContext.GetEntity<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);

        public Task<IEnumerable<TEntity>> QueryAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
            => DBContext.GetEntityAsync<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);

        public int Insert(TEntity entity, CancellationToken cancellationToken = default)
            => DBContext.Insert<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);

        public Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
            => DBContext.InsertAsync<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);

        public int InsertBatch(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
            => DBContext.InsertBatch<TPrimaryKey, TEntity>(entities, cancellationToken: cancellationToken);

        public Task<int> InsertBatchAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
            => DBContext.InsertBatchAsync<TPrimaryKey, TEntity>(entities, cancellationToken: cancellationToken);

        public int Update(TEntity entity, CancellationToken cancellationToken = default)
            => DBContext.Update<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);

        public Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
            => DBContext.UpdateAsync<TPrimaryKey, TEntity>(entity, cancellationToken: cancellationToken);

        public int UpdateBatch(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
            => DBContext.UpdateBatch<TPrimaryKey, TEntity>(entities, cancellationToken: cancellationToken);

        public Task<int> UpdateBatchAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
            => DBContext.UpdateBatchAsync<TPrimaryKey, TEntity>(entities, cancellationToken: cancellationToken);

        public int Delete(TPrimaryKey id, CancellationToken cancellationToken = default)
            => DBContext.Delete<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);

        public Task<int> DeleteAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
            => DBContext.DeleteAsync<TPrimaryKey, TEntity>(id, cancellationToken: cancellationToken);

        public int DeleteBatch(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
            => DBContext.DeleteBatch<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);

        public Task<int> DeleteBatchAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default)
            => DBContext.DeleteBatchAsync<TPrimaryKey, TEntity>(ids, cancellationToken: cancellationToken);
    }
}
