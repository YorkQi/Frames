using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.EventBus
{
    public class LocalEventBus : IEventBus
    {
        private static ConcurrentQueue<EventBusOption> Events { get; set; } = new();
        private readonly IServiceProvider serviceProvider;
        private readonly EventHandlerCollection _eventHandlerCollection = new();

        public LocalEventBus(IServiceProvider serviceProvider, EventHandlerCollection eventHandlerCollection)
        {
            this.serviceProvider = serviceProvider;
            _eventHandlerCollection = eventHandlerCollection;
        }

        /// <summary>
        /// 发布事件订阅
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public async Task Push(IEvent @event)
        {
            await Task.Run(() =>
            {
                var eventType = @event.GetType();
                if (_eventHandlerCollection.Any(t => t.EnventType == eventType))
                {
                    var eventHandler = _eventHandlerCollection.First(t => t.EnventType == eventType);
                    Events.Enqueue(new EventBusOption
                    {
                        EventHandlerType = eventHandler.EnventHandlerType,
                        Param = @event
                    });
                }
            });
        }

        internal Task Exec(CancellationToken token)
        {
            if (Events.TryDequeue(out EventBusOption? queue))
            {
                if (queue is null) throw new ApplicationException("事件总线在执行事件时未找到队列数据");
                if (queue.EventHandlerType is null) throw new ApplicationException("事件总线在执行事件时队列类型未找到");
                if (queue.Param is null) throw new ApplicationException("事件总线在执行事件时事件没有参数");
                try
                {
                    var eventHandler = serviceProvider.GetService(queue.EventHandlerType);
                    string methodName = nameof(IEventHandler<IEvent>.ExecuteAsync);
                    var method = queue.EventHandlerType.GetMethod(methodName);
                    method?.Invoke(eventHandler, new[] { queue.Param });
                }
                catch
                {
                    Console.WriteLine("事件总线在执行事件" + queue.EventHandlerType.FullName + "时发生错误");
                }
            }

            return Task.CompletedTask;
        }
    }
}
