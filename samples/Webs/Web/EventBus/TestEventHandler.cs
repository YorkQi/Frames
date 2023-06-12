using Frame.EventBus;

namespace Web.EventBus
{
    public class TestEventHandler : IEventHandler
    {

        public Task ExecuteAsync(IEvent param)
        {
            return Task.CompletedTask;
        }
    }
}
