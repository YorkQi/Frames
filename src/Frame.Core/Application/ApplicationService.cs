using System;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Uow;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Core.Application
{
    /// <summary>
    /// 应用服务基类。
    /// 提供了对工作单元（UOW）的默认支持，自动管理数据库事务生命周期。
    /// </summary>
    public abstract class ApplicationService : IApplicationService
    {
        /// <summary>
        /// 工作单元管理器。
        /// </summary>
        protected IUnitOfWorkManager UnitOfWorkManager { get; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        protected ApplicationService()
        {
            UnitOfWorkManager = null!;
        }

        /// <summary>
        /// 通过 DI 注入 UOW 管理器的构造函数。
        /// </summary>
        protected ApplicationService(IUnitOfWorkManager unitOfWorkManager)
        {
            UnitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
        }

        /// <summary>
        /// 开始一个新的工作单元。
        /// </summary>
        /// <param name="options">UOW 选项（可选）</param>
        /// <returns>工作单元实例，应使用 using 包裹</returns>
        protected IUnitOfWork BeginUnitOfWork(UnitOfWorkOptions? options = null)
        {
            return UnitOfWorkManager.Begin(options);
        }

        /// <summary>
        /// 显式提交当前 UOW（如果当前上下文有活跃 UOW 且是最外层时）。
        /// </summary>
        protected Task CompleteUnitOfWorkAsync(CancellationToken cancellationToken = default)
        {
            var current = UnitOfWorkManager.Current;
            if (current is not null && !current.IsCompleted)
                return current.CompleteAsync(cancellationToken);
            return Task.CompletedTask;
        }
    }
}
