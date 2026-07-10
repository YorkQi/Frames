using System;
using System.Diagnostics.CodeAnalysis;
using Frame.Core.Utils;

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

        [NotNull]
        public Type EventHandlerType { get; }

        [NotNull]
        public IEvent Param { get; }
    }
}
