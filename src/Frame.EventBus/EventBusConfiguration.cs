using System;
using System.Diagnostics.CodeAnalysis;
using Frame.Core;
using Frame.Core.DomainEvents;
using Frame.EventBus.Locals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Frame.EventBus
{
    /// <summary>
    /// 事件总线配置扩展方法。
    /// </summary>
    public static class EventBusConfiguration
    {
        /// <summary>
        /// 注册事件总线及相关服务。
        /// 扫描所有程序集中实现了 <see cref="IEventHandler{TEvent}"/> 的类型并自动注册。
        /// </summary>
        /// <param name="configuration">框架配置上下文</param>
        /// <param name="retryOptions">重试配置（可选），不传则使用默认值</param>
        /// <returns>配置上下文，支持链式调用</returns>
        public static ServiceConfigurationContext UseEventBus([NotNull] this ServiceConfigurationContext configuration,
            Action<EventBusRetryOptions>? retryOptions = null)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            // 构建重试配置
            var retry = new EventBusRetryOptions();
            retryOptions?.Invoke(retry);

            // 扫描并注册所有事件处理器（Scoped，与 DatabaseContext 生命周期一致）
            RegisterEventHandlers(configuration);

            // 注册事件总线单例和后台 HostedService
            configuration.Add(ServiceDescriptor.Singleton<IEventBus>(sp =>
                ActivatorUtilities.CreateInstance<LocalEventBus>(sp, retry)));
            configuration.Add(ServiceDescriptor.Singleton<IHostedService, EventBusHostService>());

            // 领域事件分发器：工作单元提交后把领域事件投递到事件总线
            configuration.Add(ServiceDescriptor.Scoped<IDomainEventDispatcher, EventBusDomainEventDispatcher>());

            return configuration;
        }

        /// <summary>
        /// 扫描已注册程序集中所有实现了 <see cref="IEventHandler{TEvent}"/> 的类型并注册。
        /// 每个 Handler 注册为 Scoped，确保与 DatabaseContext 在同一个 DI Scope 内。
        /// </summary>
        private static void RegisterEventHandlers(ServiceConfigurationContext configuration)
        {
            var eventHandlerName = typeof(IEventHandler<>).FullName;
            var assemblyTypes = configuration.GetAssemblyTypes();

            foreach (var type in assemblyTypes)
            {
                if (type.IsPublic && type.IsClass && !type.IsAbstract)
                {
                    var interfaces = type.GetInterface(eventHandlerName);
                    if (interfaces is not null)
                    {
                        var @event = interfaces.GenericTypeArguments[0];
                        configuration.Add(ServiceDescriptor.Scoped(
                            typeof(IEventHandler<>).MakeGenericType(@event), type));
                    }
                }
            }
        }
    }
}
