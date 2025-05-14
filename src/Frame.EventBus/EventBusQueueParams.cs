using Frame.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Frame.EventBus
{
    internal class EventBusQueueParams
    {
        public EventBusQueueParams([NotNull] Type eventHandlerType, [NotNull] IEvent param)
        {
            Check.NotNull(eventHandlerType, nameof(eventHandlerType));
            Check.NotNull(param, nameof(param));

            EventHandlerType = eventHandlerType;
            Param = param;
        }
        /// <summary>
        /// IEventHandler事件类型
        /// </summary>
        [NotNull]
        public Type EventHandlerType { get; set; }
        /// <summary>
        /// 执行事件使用参数
        /// </summary>
        [NotNull]
        public IEvent Param { get; set; }
    }
}
