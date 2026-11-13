using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Uow
{
    /// <summary>
    /// 工作单元管理器默认实现。
    /// 通过 AsyncLocal 维护当前异步控制流中的 UOW 实例，确保同一次请求内共享同一个 UOW 上下文。
    /// </summary>
    public class UnitOfWorkManager : IUnitOfWorkManager
    {
        private static readonly AsyncLocal<UnitOfWork?> _currentUow = new();

        /// <summary>获取当前异步上下文中的活跃 UOW（可能为 null）</summary>
        public IUnitOfWork? Current => _currentUow.Value;

        /// <summary>
        /// 开始一个新的工作单元。
        /// 如果当前上下文已有活跃 UOW，则创建嵌套 UOW 并附着到外层。
        /// </summary>
        /// <param name="options">UOW 选项。不传时使用默认值。</param>
        /// <returns>工作单元实例，建议使用 using 包裹。</returns>
        public IUnitOfWork Begin(UnitOfWorkOptions? options = null)
        {
            var uow = new UnitOfWork(this, options ?? new UnitOfWorkOptions());

            // 继承外层 UOW 的回调注册（实际提交/回滚由最外层执行）
            if (_currentUow.Value is UnitOfWork outer)
            {
                uow.OnCompleteCallback = outer.OnCompleteCallback;
                uow.OnRollbackCallback = outer.OnRollbackCallback;
            }

            // 只有最外层 UOW 才设置 AsyncLocal
            if (_currentUow.Value is null)
            {
                _currentUow.Value = uow;
            }

            return uow;
        }

        /// <summary>
        /// 注册数据库层的提交/回滚委托。
        /// 由框架数据库层（如 Frame.Databases）在初始化时调用，用于将实际 SaveChanges 逻辑挂接到 UOW。
        /// </summary>
        /// <param name="onComplete">提交回调：所有变更持久化到数据库</param>
        /// <param name="onRollback">回滚回调（可选）</param>
        public static void RegisterDbActions(
            Func<CancellationToken, Task>? onComplete,
            Action? onRollback = null)
        {
            // 注意：此处不直接设置到静态字段，而是通过每次 Begin 时传递
            // 实际存储由 DbContext 层维护
            _defaultOnComplete = onComplete;
            _defaultOnRollback = onRollback;
        }

        private static Func<CancellationToken, Task>? _defaultOnComplete;
        private static Action? _defaultOnRollback;

        /// <summary>
        /// 获取默认提交回调。由 UnitOfWork 构造函数中调用。
        /// </summary>
        internal static Func<CancellationToken, Task>? DefaultOnComplete => _defaultOnComplete;

        /// <summary>
        /// 获取默认回滚回调。
        /// </summary>
        internal static Action? DefaultOnRollback => _defaultOnRollback;

        /// <summary>清理当前 AsyncLocal 中的 UOW 引用（由 UnitOfWork 在提交/回滚后调用）</summary>
        internal static void ClearCurrent()
        {
            _currentUow.Value = null;
        }
    }
}
