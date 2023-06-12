using Frame.EventBus;

namespace Web.EventBus
{
    public class TestEvent<TEventHandler> : IEvent
        where TEventHandler : IEventHandler
    {
        public string Name { get; set; } = string.Empty;
    }
}
