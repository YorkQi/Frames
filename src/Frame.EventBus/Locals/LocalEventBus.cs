using Frame.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.EventBus
{
    public class LocalEventBus : IEventBus
    {
        private static ConcurrentQueue<EventBusQueueParams> Events { get; set; } = new();
        private readonly IServiceProvider serviceProvider;

        public LocalEventBus(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 发布事件订阅
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public async Task Push([NotNull] IEvent @event)
        {
            await Task.Run(() =>
            {
                Check.NotNull(@event, nameof(@event));
                Events.Enqueue(new EventBusQueueParams(typeof(IEventHandler<>).MakeGenericType(@event.GetType()), @event));
            });
        }

        Task IEventBus.Run(CancellationToken cancel)
        {
            if (!cancel.IsCancellationRequested)
            {
                if (Events.TryDequeue(out EventBusQueueParams queue))
                {
                    try
                    {
                        Check.NotNull(queue, nameof(queue), "执行事件未找到队列数据");
                        Check.NotNull(queue.EventHandlerType, nameof(queue.EventHandlerType), $"执行事件[{queue.EventHandlerType.FullName}]时未找到类型");
                        Check.NotNull(queue.Param, nameof(queue.Param), $"执行事件[{queue.EventHandlerType.FullName}]时未找到参数");

                        var eventHandler = serviceProvider.GetService(queue.EventHandlerType);
                        var method = queue.EventHandlerType.GetMethod(nameof(IEventHandler<IEvent>.ExecuteAsync));
                        method?.Invoke(eventHandler, [queue.Param]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"执行事件[{queue.EventHandlerType.FullName}]时发生错误:{ex.Message}");
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
