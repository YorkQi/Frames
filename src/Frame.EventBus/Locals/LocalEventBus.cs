using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.EventBus.Locals
{
    /// <summary>
    /// 本地进程内事件总线实现（基于 Channel）。
    /// </summary>
    public class LocalEventBus : IEventBus
    {
        private readonly Channel<EventBusQueueParams> _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LocalEventBus> _logger;
        private readonly EventBusRetryOptions _retryOptions;

        private static readonly ConcurrentDictionary<Type, Func<object, object, Task>> _handlerDelegateCache = new();

        public LocalEventBus(IServiceProvider serviceProvider, ILogger<LocalEventBus> logger)
            : this(serviceProvider, logger, new EventBusRetryOptions())
        {
        }

        public LocalEventBus(IServiceProvider serviceProvider, ILogger<LocalEventBus> logger, EventBusRetryOptions retryOptions)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryOptions = retryOptions ?? throw new ArgumentNullException(nameof(retryOptions));
            _channel = Channel.CreateUnbounded<EventBusQueueParams>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        }

        /// <summary>
        /// 发布事件到消息通道。
        /// </summary>
        public async Task Push([NotNull] IEvent @event)
        {
            if (@event is null) throw new ArgumentNullException(nameof(@event));
            var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());
            await _channel.Writer.WriteAsync(new EventBusQueueParams(handlerType, @event));
        }

        /// <summary>
        /// 运行事件总线后台循环（由 <c>EventBusHostService</c> 调用）。
        /// 持续读取 Channel 中的事件并处理。
        /// </summary>
        async Task IEventBus.Run(CancellationToken cancel)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(cancel))
                {
                    while (_channel.Reader.TryRead(out var queue))
                    {
                        await ProcessEventWithRetryAsync(queue, cancel);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("事件总线已正常关闭");
            }
        }

        /// <summary>
        /// 带重试机制的事件处理。按指数退避间隔重试。
        /// </summary>
        private async Task ProcessEventWithRetryAsync(EventBusQueueParams queue, CancellationToken cancel)
        {
            var maxRetries = _retryOptions.MaxRetryCount;
            var baseDelay = _retryOptions.BaseRetryDelayMs;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await ProcessEventAsync(queue);
                    return; // 成功则直接返回
                }
                catch (Exception ex) when (ex is not OperationCanceledException && attempt < maxRetries)
                {
                    // 指数退避延迟：baseDelay * 2^attempt
                    var delayMs = baseDelay * (1 << attempt);
                    _logger.LogWarning(ex,
                        "事件 [{HandlerType}] 处理失败(第 {Attempt}/{MaxRetries} 次)，{DelayMs}ms 后重试",
                        queue.EventHandlerType.FullName, attempt + 1, maxRetries, delayMs);

                    try
                    {
                        await Task.Delay(delayMs, cancel);
                    }
                    catch (OperationCanceledException)
                    {
                        return; // 应用关闭，停止重试
                    }
                }
                catch (Exception ex)
                {
                    // 最后一次尝试也失败了，记录错误但不抛异常
                    _logger.LogError(ex,
                        "事件 [{HandlerType}] 在 {Attempt} 次尝试后仍然失败，已丢弃",
                        queue.EventHandlerType.FullName, attempt + 1);
                }
            }
        }

        /// <summary>
        /// 执行单个事件处理器。
        /// 【优化】使用编译后的表达式树委托替代 MethodInfo.Invoke，性能大幅提升。
        /// </summary>
        private async Task ProcessEventAsync(EventBusQueueParams queue)
        {
            using var scope = _serviceProvider.CreateScope();
            var eventHandler = scope.ServiceProvider.GetService(queue.EventHandlerType);
            if (eventHandler is null)
            {
                _logger.LogWarning("未注册事件处理器: {HandlerType}，事件将被丢弃", queue.EventHandlerType.FullName);
                return;
            }

            // 获取或编译 ExecuteAsync 的强类型委托（编译后无反射开销）
            var handlerDelegate = _handlerDelegateCache.GetOrAdd(queue.EventHandlerType, static handlerType =>
            {
                var executeMethod = handlerType.GetMethod("ExecuteAsync",
                    BindingFlags.Instance | BindingFlags.Public);
                if (executeMethod is null)
                    throw new InvalidOperationException(
                        $"类型 {handlerType.FullName} 上未找到 ExecuteAsync 方法。请确保实现了 IEventHandler<T> 接口。");

                // 表达式树：(object target, object arg) => ((THandler)target).ExecuteAsync((TArg)arg)
                var targetParam = Expression.Parameter(typeof(object), "target");
                var argParam = Expression.Parameter(typeof(object), "arg");
                var typedTarget = Expression.Convert(targetParam, handlerType);
                var typedArg = Expression.Convert(argParam, executeMethod.GetParameters()[0].ParameterType);
                var callExpr = Expression.Call(typedTarget, executeMethod, typedArg);
                return Expression.Lambda<Func<object, object, Task>>(callExpr, targetParam, argParam).Compile();
            });

            var task = handlerDelegate(eventHandler, queue.Param);
            if (task is not null)
                await task;
        }
    }

    /// <summary>
    /// 事件总线重试配置选项。
    /// 可在 <c>UseEventBus</c> 中传入自定义配置，或直接修改默认值。
    /// </summary>
    public class EventBusRetryOptions
    {
        /// <summary>最大重试次数（默认 3 次）。设为 0 则禁重试。</summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>基础重试间隔（毫秒），实际间隔 = baseDelay * 2^attempt（指数退避）</summary>
        public int BaseRetryDelayMs { get; set; } = 1000;
    }
}
