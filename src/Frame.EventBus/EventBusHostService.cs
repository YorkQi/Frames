using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Frame.EventBus
{
    /// <summary>
    /// 事件总线后台服务。
    /// 直接监听 IEventBus.Run，不再轮询 —— Channel 到达时立即处理。
    /// </summary>
    public class EventBusHostService(IEventBus eventBus) : BackgroundService
    {

        protected override Task ExecuteAsync(CancellationToken cancel)
        {
            return eventBus.Run(cancel);
        }
    }
}
