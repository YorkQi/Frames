using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Uow
{
    /// <summary>
    /// 工作单元默认实现。
    /// 维护一个 AsyncLocal 所指向的 UOW 链，支持嵌套。
    /// 最外层 UOW 持有实际的提交/回滚回调（由数据库层注册）。
    /// </summary>
    internal sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWorkManager _manager;
        private readonly UnitOfWorkOptions _options;
        private readonly UnitOfWork? _outer;

        private bool _isCompleted;
        private bool _isRolledBack;
        private bool _disposed;
        private readonly CancellationTokenSource? _timeoutCts;

        /// <summary>
        /// 收集的外层 UOW 的所有子 UOW
        /// </summary>
        private readonly List<IUnitOfWork> _children = [];

        /// <summary>
        /// 提交处理器（注册到任意层级的 UOW 都会提升到根 UOW）
        /// </summary>
        private readonly List<Func<CancellationToken, Task>> _completionHandlers = [];

        /// <summary>
        /// 回滚处理器（注册到任意层级的 UOW 都会提升到根 UOW）
        /// </summary>
        private readonly List<Action> _rollbackHandlers = [];

        /// <summary>
        /// UOW 标识，用于日志和调试追踪
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString("N");

        public bool IsCompleted => _isCompleted;
        public bool IsRolledBack => _isRolledBack;

        /// <summary>
        /// 工作单元共享数据。嵌套 UOW 与最外层共享同一个字典，便于跨层传递（例如挂载 IDbContext）。
        /// </summary>
        private readonly Dictionary<string, object?> _items = new();

        /// <inheritdoc />
        public IDictionary<string, object?> Items => Root._items;

        /// <summary>根 UOW（最外层）。</summary>
        private UnitOfWork Root
        {
            get
            {
                var current = this;
                while (current._outer is not null)
                {
                    current = current._outer;
                }
                return current;
            }
        }

        /// <inheritdoc />
        public void RegisterCompletionHandler(Func<CancellationToken, Task> handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var root = Root;
            lock (root._completionHandlers)
            {
                root._completionHandlers.Add(handler);
            }
        }

        /// <inheritdoc />
        public void RegisterRollbackHandler(Action handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var root = Root;
            lock (root._rollbackHandlers)
            {
                root._rollbackHandlers.Add(handler);
            }
        }

        /// <summary>
        /// 提交时回调，由 UnitOfWorkManager 的静态委托注册（数据库层注入）。
        /// 最外层 UOW 提交时调用。
        /// </summary>
        internal Func<CancellationToken, Task>? OnCompleteCallback { get; set; }

        /// <summary>
        /// 回滚时回调，由 UnitOfWorkManager 的静态委托注册（数据库层注入）。
        /// 最外层 UOW 回滚时调用。
        /// </summary>
        internal Action? OnRollbackCallback { get; set; }

        public UnitOfWork(IUnitOfWorkManager manager, UnitOfWorkOptions options)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _options = options ?? new UnitOfWorkOptions();
            _outer = manager.Current as UnitOfWork;

            if (_outer is not null)
            {
                // 有外层 UOW：注册为子 UOW，不修改 AsyncLocal
                lock (_outer._children)
                {
                    _outer._children.Add(this);
                }
            }

            // 如果设置了超时，启动超时计时器
            if (_options.Timeout.HasValue && _options.Timeout.Value > TimeSpan.Zero)
            {
                _timeoutCts = new CancellationTokenSource(_options.Timeout.Value);
                _timeoutCts.Token.Register(async () =>
                {
                    if (!_isCompleted && !_isRolledBack)
                        await RollbackAsync();
                });
            }
        }

        /// <summary>
        /// 提交当前工作单元。
        /// - 嵌套 UOW：仅标记完成，不触发回调
        /// - 最外层 UOW：先提交所有子 UOW，再执行 OnCompleteCallback
        /// </summary>
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (_isCompleted) return;
            if (_isRolledBack)
                throw new InvalidOperationException($"工作单元 {Id} 已回滚，无法提交。");

            // 嵌套 UOW：合并到外层，由外层统一提交
            if (_outer is not null && !_outer.IsCompleted)
            {
                _isCompleted = true;
                return;
            }

            // 最外层 UOW：先完成所有子 UOW
            List<IUnitOfWork> childrenSnapshot;
            lock (_children)
            {
                childrenSnapshot = [.. _children];
            }

            foreach (var child in childrenSnapshot)
            {
                if (!child.IsCompleted && !child.IsRolledBack)
                    await child.CompleteAsync(cancellationToken);
            }

            // 执行提交处理器（开启事务 → SaveChanges → 提交 → 分发领域事件）
            List<Func<CancellationToken, Task>> handlers;
            lock (_completionHandlers)
            {
                handlers = [.. _completionHandlers];
            }

            foreach (var handler in handlers)
            {
                await handler(cancellationToken);
            }

            // 执行实际提交回调（SaveChanges / 事务提交）
            if (OnCompleteCallback is not null)
            {
                await OnCompleteCallback(cancellationToken);
            }

            _isCompleted = true;
            _timeoutCts?.Dispose();

            // 清理 AsyncLocal 引用
            if (_outer is null)
            {
                UnitOfWorkManager.ClearCurrent();
            }
        }

        /// <summary>
        /// 回滚当前工作单元。
        /// - 嵌套 UOW：冒泡到外层 UOW 执行回滚
        /// - 最外层 UOW：执行 OnRollbackCallback
        /// </summary>
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_isRolledBack || _isCompleted) return Task.CompletedTask;

            _isRolledBack = true;

            if (_outer is not null && !_outer.IsRolledBack)
            {
                // 冒泡到外层执行回滚
                return _outer.RollbackAsync(cancellationToken);
            }

            // 最外层：按注册逆序执行回滚处理器
            List<Action> rollbackHandlers;
            lock (_rollbackHandlers)
            {
                rollbackHandlers = [.. _rollbackHandlers];
            }

            for (int i = rollbackHandlers.Count - 1; i >= 0; i--)
            {
                rollbackHandlers[i]();
            }

            // 最外层：执行回滚回调
            OnRollbackCallback?.Invoke();

            _timeoutCts?.Dispose();

            UnitOfWorkManager.ClearCurrent();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (!_isCompleted && !_isRolledBack)
            {
                if (_options.IsAutoRollback)
                {
                    RollbackAsync().GetAwaiter().GetResult();
                }
                // 默认策略：未提交也未显式回滚时，自动回滚以防止悬挂事务
                else
                {
                    RollbackAsync().GetAwaiter().GetResult();
                }
            }

            // 从外层 UOW 的子列表中移除自身
            if (_outer is not null)
            {
                lock (_outer._children)
                {
                    _outer._children.Remove(this);
                }
            }

            _timeoutCts?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            if (!_isCompleted && !_isRolledBack)
            {
                if (_options.IsAutoRollback)
                {
                    await RollbackAsync();
                }
                else
                {
                    await RollbackAsync();
                }
            }

            if (_outer is not null)
            {
                lock (_outer._children)
                {
                    _outer._children.Remove(this);
                }
            }

            _timeoutCts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
