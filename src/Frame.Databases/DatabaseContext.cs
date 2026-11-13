using System;
using System.Threading.Tasks;
using Frame.Core.Entities;
using Frame.Core.Repositories;
using Frame.Databases.DbContexts;
using Frame.Databases.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Databases
{
    /// <summary>
    /// 数据库请求上下文。
    /// 管理同一次请求中所有数据库操作的生命周期，确保使用同一个 Scope 内的 IDbContext 实例，
    /// 使 EF Core 变更追踪和 UOW 正常工作。
    /// </summary>
    public class DatabaseContext : IDisposable, IAsyncDisposable
    {
        private IServiceProvider _provider = default!;
        private string _dbConnectionString = default!;
        private IServiceScope? _scope;
        private IDbContext? _dbContext;
        private bool disposedValue;

        internal void Initialize(IServiceProvider provider, ConnectionStringCollection dbConnectionStrings,
            ConnectionStringStrategy strategy)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(dbConnectionStrings);
            _provider = provider;
            _dbConnectionString = dbConnectionStrings.GetConnection(strategy);
        }

        public TRepository GetRepository<TRepository>() where TRepository : IRepository
        {
            return GetOrCreateScope().ServiceProvider.GetRequiredService<TRepository>();
        }

        public IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity
        {
            return GetOrCreateScope().ServiceProvider.GetRequiredService<IRepository<TPrimaryKey, TEntity>>();
        }
        private IServiceScope GetOrCreateScope()
        {
            if (_scope is not null)
                return _scope;

            _scope = _provider.CreateScope();
            var accessor = _scope.ServiceProvider.GetRequiredService<DbContextConnectionStringAccessor>();
            accessor.ConnectionString = _dbConnectionString;            
            _dbContext = _scope.ServiceProvider.GetRequiredService<IDbContext>();
            DbContextScope.Set(_dbContext);

            return _scope;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _scope?.Dispose();
                }

                _scope = null;
                _dbContext = null;
                DbContextScope.Clear();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                if (_scope is IAsyncDisposable asyncScope)
                    await asyncScope.DisposeAsync();
                else
                    _scope?.Dispose();

                _scope = null;
                _dbContext = null;
                DbContextScope.Clear();
                disposedValue = true;
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
