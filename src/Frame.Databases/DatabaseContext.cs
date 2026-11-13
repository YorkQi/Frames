using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.DomainEvents;
using Frame.Core.Entities;
using Frame.Core.Repositories;
using Frame.Core.Uow;
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
    /// <remarks>
    /// 当容器中存在工作单元时，本上下文会把 <see cref="IDbContext"/> 挂到
    /// <see cref="IUnitOfWork.Items"/>，并注册提交/回滚处理器：
    /// 提交时依次「收集领域事件 → 开启事务 → SaveChanges → 提交 → 分发事件」。
    /// </remarks>
    public class DatabaseContext : IDisposable, IAsyncDisposable
    {
        /// <summary>工作单元 Items 中存放 IDbContext 的键。</summary>
        public const string DbContextItemKey = "Frame.Databases.IDbContext";

        private IServiceProvider _provider = default!;
        private string _dbConnectionString = default!;
        private IUnitOfWorkManager? _unitOfWorkManager;
        private IServiceScope? _scope;
        private IDbContext? _dbContext;
        private bool _attachedToUnitOfWork;
        private bool disposedValue;

        internal void Initialize(IServiceProvider provider, ConnectionStringCollection dbConnectionStrings,
            ConnectionStringStrategy strategy)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(dbConnectionStrings);
            _provider = provider;
            _unitOfWorkManager = provider.GetService<IUnitOfWorkManager>();
            _dbConnectionString = dbConnectionStrings.GetConnection(strategy);
        }

        public TRepository GetRepository<TRepository>() where TRepository : IRepository
        {
            var scope = GetOrCreateScope();
            AttachToUnitOfWork();
            return scope.ServiceProvider.GetRequiredService<TRepository>();
        }

        public IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity
        {
            var scope = GetOrCreateScope();
            AttachToUnitOfWork();
            return scope.ServiceProvider.GetRequiredService<IRepository<TPrimaryKey, TEntity>>();
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

        /// <summary>
        /// 把当前 IDbContext 挂到活动工作单元，并注册提交/回滚处理器（每个 DatabaseContext 只注册一次）。
        /// 没有活动工作单元时不做事，调用方需自行 SaveChanges。
        /// </summary>
        private void AttachToUnitOfWork()
        {
            if (_attachedToUnitOfWork || _dbContext is null)
                return;

            var unitOfWork = _unitOfWorkManager?.Current;
            if (unitOfWork is null)
                return;

            _attachedToUnitOfWork = true;

            var dbContext = _dbContext;
            unitOfWork.Items[DbContextItemKey] = dbContext;

            unitOfWork.RegisterCompletionHandler(ct => FlushAndDispatchAsync(dbContext, ct));
            unitOfWork.RegisterRollbackHandler(() => dbContext.Rollback());
        }

        private async Task FlushAndDispatchAsync(IDbContext dbContext, CancellationToken cancellationToken)
        {
            // 1. 收集挂起的领域事件。事件来源是「所有被追踪的实体」而不是「已变更实体」：
            //    应用服务可能为了拿到自增主键而显式 SaveChanges，此时实体已变成 Unchanged，
            //    但它的事件仍需要在本工作单元提交后分发。
            var changedEntities = dbContext.GetChangedEntities();
            var eventSources = dbContext.GetTrackedEntities()
                .OfType<IHasDomainEvents>()
                .Where(e => e.DomainEvents is { Count: > 0 })
                .ToList();
            var domainEvents = eventSources
                .SelectMany(e => e.DomainEvents ?? Array.Empty<object>())
                .ToList();

            if (changedEntities.Count == 0 && domainEvents.Count == 0)
                return;

            // 2. 事务内落库
            if (changedEntities.Count > 0)
            {
                await dbContext.BeginTransactionAsync(cancellationToken: cancellationToken);
                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await dbContext.CommitAsync(cancellationToken);
                }
                catch
                {
                    await dbContext.RollbackAsync(cancellationToken);
                    throw;
                }
            }

            // 3. 事务提交成功后再分发领域事件，避免回滚后事件已经外发
            if (domainEvents.Count > 0)
            {
                var dispatcher = _provider.GetService<IDomainEventDispatcher>();
                if (dispatcher is not null)
                {
                    foreach (var domainEvent in domainEvents)
                    {
                        await dispatcher.DispatchAsync(domainEvent, cancellationToken);
                    }
                }
            }

            foreach (var source in eventSources)
            {
                source.ClearDomainEvents();
            }
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
