using Frame.EventBus;
using Xunit;

namespace TestUnit
{
    public class EventBusInterfacesTest
    {
        [Fact]
        public void IEvent_CanBeImplemented()
        {
            var evt = new TestEvent();
            Assert.IsAssignableFrom<IEvent>(evt);
        }

        [Fact]
        public void IEventHandler_CanBeImplemented()
        {
            var handler = new TestEventHandler();
            Assert.IsAssignableFrom<IEventHandler<TestEvent>>(handler);
        }

        private class TestEvent : IEvent { }

        private class TestEventHandler : IEventHandler<TestEvent>
        {
            public Task ExecuteAsync(TestEvent param) => Task.CompletedTask;
        }
    }
}
