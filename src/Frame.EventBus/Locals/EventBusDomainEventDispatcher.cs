using System;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.DomainEvents;
using Microsoft.Extensions.Logging;

namespace Frame.EventBus.Locals
{
    /// <summary>
    /// 把领域事件交给进程内事件总线分发。
    /// 只有实现了 <see cref="IEvent"/> 的领域事件会被投递，其余记录警告后丢弃。
    /// </summary>
    public sealed class EventBusDomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<EventBusDomainEventDispatcher>? _logger;

        /// <summary>构造函数。</summary>
        /// <param name="eventBus">事件总线</param>
        /// <param name="logger">日志（可选）</param>
        public EventBusDomainEventDispatcher(IEventBus eventBus, ILogger<EventBusDomainEventDispatcher>? logger = null)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger;
        }

        /// <inheritdoc />
        public Task DispatchAsync(object domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent is null) throw new ArgumentNullException(nameof(domainEvent));

            if (domainEvent is not IEvent @event)
            {
                _logger?.LogWarning("领域事件 {EventType} 未实现 IEvent，已忽略。", domainEvent.GetType().FullName);
                return Task.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return _eventBus.Push(@event);
        }
    }
}
