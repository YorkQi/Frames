using System;
using System.Diagnostics.CodeAnalysis;

namespace Frame.EventBus
{
    internal class EventBusQueueParams
    {
        public EventBusQueueParams([NotNull] Type eventHandlerType, [NotNull] IEvent param)
        {
            if (eventHandlerType is null) throw new ArgumentNullException(nameof(eventHandlerType));
            if (param is null) throw new ArgumentNullException(nameof(param));

            EventHandlerType = eventHandlerType;
            Param = param;
        }

        [NotNull]
        public Type EventHandlerType { get; }

        [NotNull]
        public IEvent Param { get; }
    }
}
