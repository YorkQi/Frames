using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Frame.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.EventBus.Locals
{
    public class LocalEventBus : IEventBus
    {
        private readonly Channel<EventBusQueueParams> _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LocalEventBus> _logger;

        /// <summary>缓存 IEventHandler&lt;T&gt;.ExecuteAsync 的 MethodInfo，避免每次反射</summary>
        private static readonly ConcurrentDictionary<Type, MethodInfo> _handlerMethodCache = new();

        public LocalEventBus(IServiceProvider serviceProvider, ILogger<LocalEventBus> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _channel = Channel.CreateUnbounded<EventBusQueueParams>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        }

        /// <summary>
        /// 发布事件（订阅）
        /// </summary>
        public async Task Push([NotNull] IEvent @event)
        {
            Check.NotNull(@event, nameof(@event));
            var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());
            await _channel.Writer.WriteAsync(new EventBusQueueParams(handlerType, @event));
        }

        async Task IEventBus.Run(CancellationToken cancel)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(cancel))
                {
                    while (_channel.Reader.TryRead(out var queue))
                    {
                        await ProcessEventAsync(queue);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常关闭
            }
        }

        private async Task ProcessEventAsync(EventBusQueueParams queue)
        {
            try
            {
                // 通过 Scope 解析 Transient Handler，避免从根容器解析导致 Captive Dependency
                using var scope = _serviceProvider.CreateScope();
                var eventHandler = scope.ServiceProvider.GetService(queue.EventHandlerType);
                if (eventHandler is null)
                {
                    _logger.LogWarning("未注册事件处理器: {HandlerType}", queue.EventHandlerType.FullName);
                    return;
                }

                var method = _handlerMethodCache.GetOrAdd(queue.EventHandlerType, type =>
                {
                    var m = type.GetMethod(nameof(IEventHandler<>.ExecuteAsync))
                        ?? type.GetMethod("ExecuteAsync");
                    return m ?? throw new InvalidOperationException(
                        $"类型 {type.FullName} 上找不到 ExecuteAsync 方法");
                });

                var task = method.Invoke(eventHandler, [queue.Param]) as Task;
                if (task is not null)
                {
                    await task;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "执行事件[{EventHandlerType}]时发生错误", queue.EventHandlerType.FullName);
            }
        }
    }
}
