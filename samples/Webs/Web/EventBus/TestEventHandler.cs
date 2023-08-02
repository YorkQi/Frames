using Frame.EventBus;

namespace Web.EventBus
{
    public class TestEventHandler : IEventHandler<TestEvent>
    {
        public Task ExecuteAsync(TestEvent param)
        {
            Console.WriteLine(param.Name);
            return Task.CompletedTask;
        }
    }
}
