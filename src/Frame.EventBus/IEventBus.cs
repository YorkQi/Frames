using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.EventBus
{
    public interface IEventBus
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        Task Push([NotNull] IEvent @event);

        /// <summary>
        /// 执行事件（内部使用）
        /// </summary>
        internal Task Run(CancellationToken cancel);
    }
}
