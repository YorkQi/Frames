using Frame.EventBus;

namespace Web.EventBus
{
    public class TestEvent : IEvent
    {
        public string Name { get; set; } = string.Empty;
    }
}
