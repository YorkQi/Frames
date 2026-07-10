using System;
using System.Threading.Tasks;
using Frame.Core.Entities;
using Frame.Core.Repositories;
using Frame.Core.Utils;
using Frame.Databases.DbContexts;
using Frame.Databases.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Databases
{
    /// <summary>
    /// 数据库请求上下文。
    /// PS：统一请求共用用一个DBContext，避免每次获取 Repository 都创建新的 DbContext，导致 EF Core 变更追踪失效。
    /// 每次获取 Repository 复用同一个内部 Scope，生命周期与 DatabaseContext 一致。
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
            Check.NotNull(provider, nameof(provider));
            Check.NotNull(dbConnectionStrings, nameof(dbConnectionStrings));
            Check.NotNull(strategy, nameof(strategy));
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

            // 设置连接字符串，DbContext 构造器通过 ConnectionStringAccessor 读取
            _scope.ServiceProvider.GetRequiredService<DbContextConnectionStringAccessor>().ConnectionString = _dbConnectionString;
            _dbContext = _scope.ServiceProvider.GetRequiredService<IDbContext>();
            DbContextAccessor.Set(_dbContext);

            return _scope;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 先清除 Ambient Context，避免后续代码访问到已释放的 IDbContext
                    DbContextAccessor.Clear();
                    _scope?.Dispose();
                }

                _scope = null;
                _dbContext = null;
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
                // 先清除 Ambient Context，避免后续代码访问到已释放的 IDbContext
                DbContextAccessor.Clear();
                if (_scope is IAsyncDisposable asyncScope)
                {
                    await asyncScope.DisposeAsync();
                }
                else
                {
                    _scope?.Dispose();
                }
                _scope = null;
                _dbContext = null;
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
