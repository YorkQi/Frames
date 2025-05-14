using Microsoft.Extensions.Hosting;
using System.Threading;

using System.Threading.Tasks;

namespace Frame.EventBus
{
    public class EventBusHostService(IEventBus eventBus) : BackgroundService
    {
        private readonly int interval = 100;//间隔100毫秒
        private readonly IEventBus eventBus = eventBus;

        protected override async Task ExecuteAsync(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                await eventBus.Run(cancel);

                await Task.Delay(interval, cancel);
            }
        }
    }
}
