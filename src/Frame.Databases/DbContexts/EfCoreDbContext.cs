using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Entities;
using Frame.Databases.DbContexts;
using Frame.Databases.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frame.Databases.DBContext
{
    /// <summary>
    /// IDBContext 实现。
    /// 实体 CRUD 走 EF Core（变更追踪、关系修正）
    /// </summary>
    public partial class DbContext : IDbContext, IDisposable, IAsyncDisposable
    {
        private readonly EfCoreContext _dbContext;
        private IDbContextTransaction? _efTransaction;

        /// <summary>缓存实体类型 → 表名，避免每次查询 EF 元数据</summary>
        private static readonly ConcurrentDictionary<Type, string> _tableNameCache = new();



        public DbContext(DbContextConnectionStringAccessor connectionStringAccessor)
        {
            var connectionString = connectionStringAccessor.ConnectionString
                ?? throw new InvalidOperationException("连接字符串未设置。请先设置 ConnectionStringAccessor。");

            var optionsBuilder = new DbContextOptionsBuilder<EfCoreContext>();
            ConfigureProvider(optionsBuilder, connectionString);

            _dbContext = new EfCoreContext(optionsBuilder.Options);
        }

        /// <summary>
        /// 配置数据库 Provider。默认使用 MySQL (Pomelo)。
        /// 子类可重写以支持其他数据库。
        /// </summary>
        protected virtual void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            optionsBuilder.UseMySql(connectionString, serverVersion);
        }

        // ---- 底层访问 ----

        public DbConnection GetDbConnection(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }

        public async Task<DbConnection> GetDbConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);
            return conn;
        }

        // ---- 事务管理 ----

        public void BeginTransaction(IsolationLevel level = IsolationLevel.Unspecified, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetDbConnection(cancellationToken); // 确保连接已打开
            _efTransaction = _dbContext.Database.BeginTransaction(level);
        }

        public async Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.Unspecified, CancellationToken cancellationToken = default)
        {
            await GetDbConnectionAsync(cancellationToken); // 确保连接已打开
            _efTransaction = await _dbContext.Database.BeginTransactionAsync(level, cancellationToken);
        }

        public void Commit(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_efTransaction != null)
            {
                _efTransaction.Commit();
                _efTransaction.Dispose();
                _efTransaction = null;
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_efTransaction != null)
            {
                await _efTransaction.CommitAsync(cancellationToken);
                await _efTransaction.DisposeAsync();
                _efTransaction = null;
            }
        }

        public void Rollback(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_efTransaction != null)
            {
                _efTransaction.Rollback();
                _efTransaction.Dispose();
                _efTransaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_efTransaction != null)
            {
                await _efTransaction.RollbackAsync(cancellationToken);
                await _efTransaction.DisposeAsync();
                _efTransaction = null;
            }
        }

        private DbTransaction? GetDbTransaction()
        {
            return _efTransaction?.GetDbTransaction();
        }

        // ---- 表名辅助方法 ----

        /// <summary>
        /// 获取实体对应的数据库表名（优先读取 [Table] 特性，其次 EF 元数据，最后兜底类名）
        /// </summary>
        protected virtual string GetTableName<TEntity>()
        {
            return _tableNameCache.GetOrAdd(typeof(TEntity), type =>
            {
                // 1. 尝试从 EF Core 模型元数据获取
                var entityType = _dbContext.Model.FindEntityType(type);
                if (entityType is not null)
                {
                    var schema = entityType.GetSchema();
                    var tableName = entityType.GetTableName();
                    if (!string.IsNullOrWhiteSpace(tableName))
                    {
                        return string.IsNullOrWhiteSpace(schema)
                            ? $"`{tableName}`"
                            : $"`{schema}`.`{tableName}`";
                    }
                }

                // 2. 兜底：使用类型名称（C# 类型名通常与表名一致）
                return $"`{type.Name}`";
            });
        }

        // ---- ID 转换辅助方法 ----

        /// <summary>
        /// 将 ids 拍平为 List，用于 Dapper 参数化 IN 查询。
        /// Dapper 会自动展开集合参数（WHERE Id IN @ids → WHERE Id IN (@ids1, @ids2, ...)）。
        /// </summary>
        private static List<object> FlattenIds(object ids)
        {
            if (ids is string)
                return [ids];

            if (ids is IEnumerable enumerable)
                return [.. enumerable.Cast<object>()];

            return [ids];
        }

        // ===========================================================================
        // IDBContext 实现 — Insert / Update / GetAll（不直接依赖主键值）
        // ===========================================================================

        public IEnumerable<TEntity> GetAllEntity<TPrimaryKey, TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tableName = GetTableName<TEntity>();
            return Query<TEntity>($"SELECT * FROM {tableName};", cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<TEntity>> GetAllEntityAsync<TPrimaryKey, TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            var tableName = GetTableName<TEntity>();
            return await this.QueryAsync<TEntity>($"SELECT * FROM {tableName};", cancellationToken: cancellationToken);
        }

        public int Insert<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbContext.Add(entity!);
            return _dbContext.SaveChanges();
        }

        public async Task<int> InsertAsync<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            _dbContext.Add(entity!);
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public int InsertBatch<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbContext.AddRange(entities.Cast<object>());
            return _dbContext.SaveChanges();
        }

        public async Task<int> InsertBatchAsync<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            _dbContext.AddRange(entities.Cast<object>());
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public int Update<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbContext.Update(entity!);
            return _dbContext.SaveChanges();
        }

        public async Task<int> UpdateAsync<TPrimaryKey, TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            _dbContext.Update(entity!);
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public int UpdateBatch<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbContext.UpdateRange(entities.Cast<object>());
            return _dbContext.SaveChanges();
        }

        public async Task<int> UpdateBatchAsync<TPrimaryKey, TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            _dbContext.UpdateRange(entities.Cast<object>());
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // ===========================================================================
        // IDBContext 实现 — Get / Delete（依赖主键值）
        // ===========================================================================

        public TEntity Get<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return GetCore<TEntity>(id!, cancellationToken);
        }

        public async Task<TEntity> GetAsync<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return await GetCoreAsync<TEntity>(id!, cancellationToken);
        }

        public IEnumerable<TEntity> GetEntity<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return GetEntityCore<TEntity>(ids!, cancellationToken);
        }

        public async Task<IEnumerable<TEntity>> GetEntityAsync<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return await GetEntityCoreAsync<TEntity>(ids!, cancellationToken);
        }

        public int Delete<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return DeleteCore<TEntity>(id!, cancellationToken);
        }

        public async Task<int> DeleteAsync<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return await DeleteCoreAsync<TEntity>(id!, cancellationToken);
        }

        public int DeleteBatch<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return DeleteBatchCore<TEntity>(ids!, cancellationToken);
        }

        public async Task<int> DeleteBatchAsync<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>
        {
            return await DeleteBatchCoreAsync<TEntity>(ids!, cancellationToken);
        }

        // ===========================================================================
        // IDbContext EF Core 执行部分 实现
        // ===========================================================================

        public TEntity? Find<TEntity>(params object[] keys) where TEntity : class, IEntity
        {
            return _dbContext.Find<TEntity>(keys);
        }

        public ValueTask<TEntity?> FindAsync<TEntity>(params object[] keys) where TEntity : class, IEntity
        {
            return _dbContext.FindAsync<TEntity>(keys);
        }

        public IQueryable<TEntity> Queryable<TEntity>() where TEntity : class, IEntity
        {
            return _dbContext.Set<TEntity>();
        }

        public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class, IEntity
        {
            return _dbContext.Entry(entity);
        }

        public EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class, IEntity
        {
            return _dbContext.Attach(entity);
        }

        public void Detach<TEntity>(TEntity entity) where TEntity : class, IEntity
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
        }

        public int SaveChanges(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _dbContext.SaveChanges();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }


        // ===========================================================================
        // Internal Core 实现（object id，各类型方法委托至此）
        // ===========================================================================

        private TEntity GetCore<TEntity>(object id, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entity = _dbContext.Find(typeof(TEntity), id);
            return (TEntity)entity!;
        }

        private async Task<TEntity> GetCoreAsync<TEntity>(object id, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync([id], cancellationToken).AsTask();
            return (TEntity)entity!;
        }

        private IEnumerable<TEntity> GetEntityCore<TEntity>(object ids, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tableName = GetTableName<TEntity>();
            var idList = FlattenIds(ids);
            return this.Query<TEntity>(
                $"SELECT * FROM {tableName} WHERE Id IN @ids;",
                new { ids = idList },
                cancellationToken: cancellationToken);
        }

        private async Task<IEnumerable<TEntity>> GetEntityCoreAsync<TEntity>(object ids, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            var tableName = GetTableName<TEntity>();
            var idList = FlattenIds(ids);
            return await this.QueryAsync<TEntity>(
                $"SELECT * FROM {tableName} WHERE Id IN @ids;",
                new { ids = idList },
                cancellationToken: cancellationToken);
        }

        private int DeleteCore<TEntity>(object id, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entity = _dbContext.Find(typeof(TEntity), id);
            if (entity != null)
            {
                _dbContext.Remove(entity);
                return _dbContext.SaveChanges();
            }
            return 0;
        }

        private async Task<int> DeleteCoreAsync<TEntity>(object id, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync([id], cancellationToken).AsTask();
            if (entity != null)
            {
                _dbContext.Remove(entity);
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return 0;
        }

        private int DeleteBatchCore<TEntity>(object ids, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tableName = GetTableName<TEntity>();
            var idList = FlattenIds(ids);
            return this.Execute(
                $"DELETE FROM {tableName} WHERE Id IN @ids;",
                new { ids = idList },
                cancellationToken: cancellationToken);
        }

        private async Task<int> DeleteBatchCoreAsync<TEntity>(object ids, CancellationToken cancellationToken) where TEntity : class, IEntity
        {
            var tableName = GetTableName<TEntity>();
            var idList = FlattenIds(ids);
            return await this.ExecuteAsync(
                $"DELETE FROM {tableName} WHERE Id IN @ids;",
                new { ids = idList },
                cancellationToken: cancellationToken);
        }

        // ---- IDisposable ----

        // 私有标记，防止重复释放
        private bool _disposed;

        // 显式接口实现 IDisposable
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // 统一同步释放逻辑
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                // 同步释放事务
                _efTransaction?.Dispose();
                // 同步释放DB上下文
                _dbContext?.Dispose();
            }

            _efTransaction = null;
        }

        // 异步释放（IAsyncDisposable）
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            // 1. 异步释放事务
            if (_efTransaction is IAsyncDisposable asyncTx)
            {
                await asyncTx.DisposeAsync();
            }
            else
            {
                _efTransaction?.Dispose();
            }

            // 2. 异步释放DBContext（IDBContext 实现 IAsyncDisposable）
            if (_dbContext is IAsyncDisposable asyncDb)
            {
                await asyncDb.DisposeAsync();
            }
            else
            {
                _dbContext?.Dispose();
            }

            // 清空引用
            _efTransaction = null;

            GC.SuppressFinalize(this);
        }
    }
}
