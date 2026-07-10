using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Entities;
using Frame.Core.Entities.Dtos;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Frame.Databases
{
    public interface IDbContext
    {
        #region 底层连接
        DbConnection GetDbConnection(CancellationToken cancellationToken = default);
        Task<DbConnection> GetDbConnectionAsync(CancellationToken cancellationToken = default);
        #endregion

        #region 事务
        void BeginTransaction(IsolationLevel level = IsolationLevel.Unspecified, CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.Unspecified, CancellationToken cancellationToken = default);
        void Commit(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        void Rollback(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
        #endregion

        #region 框架执行部分

        TEntity Get<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<TEntity> GetAsync<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        IEnumerable<TEntity> GetEntity<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<IEnumerable<TEntity>> GetEntityAsync<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        IEnumerable<TEntity> GetAllEntity<TPrimaryKey, TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<IEnumerable<TEntity>> GetAllEntityAsync<TPrimaryKey, TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;

        int Insert<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<int> InsertAsync<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        int InsertBatch<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<int> InsertBatchAsync<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;

        int Update<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<int> UpdateAsync<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        int UpdateBatch<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<int> UpdateBatchAsync<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        int Delete<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<int> DeleteAsync<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        int DeleteBatch<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;
        Task<int> DeleteBatchAsync<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>;

        #endregion

        #region Sql执行部分

        /// <summary>执行 SQL 查询，返回多行结果集</summary>
        IEnumerable<T> Query<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL 查询，返回多行结果集</summary>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>执行 SQL 查询，返回第一行（无结果时抛异常）</summary>
        T QueryFirst<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL 查询，返回第一行（无结果时抛异常）</summary>
        Task<T> QueryFirstAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>执行 SQL 查询，返回第一行或默认值</summary>
        T? QueryFirstOrDefault<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL 查询，返回第一行或默认值</summary>
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>执行 SQL 查询，返回单行（多行或无结果时抛异常）</summary>
        T QuerySingle<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL 查询，返回单行（多行或无结果时抛异常）</summary>
        Task<T> QuerySingleAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>执行 SQL 查询，返回单行或默认值（多行时抛异常）</summary>
        T? QuerySingleOrDefault<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL 查询，返回单行或默认值（多行时抛异常）</summary>
        Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>执行 SQL，返回受影响行数</summary>
        int Execute(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL，返回受影响行数</summary>
        Task<int> ExecuteAsync(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>执行 SQL，返回首行首列值</summary>
        T ExecuteScalar<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL，返回首行首列值</summary>
        Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>执行 SQL，返回多结果集读取器</summary>
        IDBContextReader QueryMultiple(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步执行 SQL，返回多结果集读取器</summary>
        Task<IDBContextReader> QueryMultipleAsync(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>分页查询：一次往返完成 COUNT + 数据查询，返回 <see cref="PageResult{T}"/></summary>
        PageResult<T> QueryPaged<T>(string sql, object? param = null, int page = 1, int size = 20, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
        /// <summary>异步分页查询：一次往返完成 COUNT + 数据查询，返回 <see cref="PageResult{T}"/></summary>
        Task<PageResult<T>> QueryPagedAsync<T>(string sql, object? param = null, int page = 1, int size = 20, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        #endregion

        #region EF Core 执行部分

        /// <summary>按主键查找实体，返回 null 表示未找到（EF Core Find）</summary>
        TEntity? Find<TEntity>(params object[] keys) where TEntity : class, IEntity;

        /// <summary>异步按主键查找实体，返回 null 表示未找到（EF Core FindAsync）</summary>
        ValueTask<TEntity?> FindAsync<TEntity>(params object[] keys) where TEntity : class, IEntity;

        /// <summary>获取实体的 IQueryable，用于构建 LINQ 查询（Where、Include、Select 等）</summary>
        IQueryable<TEntity> Queryable<TEntity>() where TEntity : class, IEntity;

        /// <summary>获取实体的 EntityEntry，用于精细控制变更状态</summary>
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class, IEntity;

        /// <summary>将实体附加到变更追踪器，状态设为 Unchanged</summary>
        EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class, IEntity;

        /// <summary>将实体从变更追踪器中分离</summary>
        void Detach<TEntity>(TEntity entity) where TEntity : class, IEntity;

        /// <summary>显式保存所有挂起的变更（通常 Insert/Update/Delete 已自动保存，此方法用于批量操作场景）</summary>
        int SaveChanges(CancellationToken cancellationToken = default);

        /// <summary>异步显式保存所有挂起的变更</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        #endregion
    }
}
