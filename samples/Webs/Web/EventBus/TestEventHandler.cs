using Frame.EventBus;
using System.Diagnostics;

namespace Web.EventBus
{
    public class TestEventHandler : IEventHandler<TestEvent>
    {
        public Task ExecuteAsync(TestEvent param)
        {
            Debug.WriteLine(param.Name);
            Console.WriteLine(param.Name);
            return Task.CompletedTask;
        }
    }
}
