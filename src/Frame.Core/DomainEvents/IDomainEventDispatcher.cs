using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.DomainEvents
{
    /// <summary>
    /// 领域事件分发器。工作单元在持久化成功后，把实体上收集到的领域事件交给它。
    /// 框架默认不注册实现；启用 <c>UseEventBus()</c> 后由事件总线模块提供实现。
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>
        /// 分发一个领域事件。
        /// </summary>
        /// <param name="domainEvent">事件对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task DispatchAsync(object domainEvent, CancellationToken cancellationToken = default);
    }
}
