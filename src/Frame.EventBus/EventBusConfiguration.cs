using System.Diagnostics.CodeAnalysis;
using Frame.Core;
using Frame.Core.Utils;
using Frame.EventBus.Locals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Frame.EventBus
{
    public static class EventBusConfiguration
    {
        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ServiceConfigurationContext UseEventBus([NotNull] this ServiceConfigurationContext configuration)
        {

            Check.NotNull(configuration, nameof(configuration));

            var eventHandlerName = typeof(IEventHandler<>).FullName;

            #region 注入事件操作类
            var assemblyTypes = configuration.GetAssemblyType();
            foreach (var type in assemblyTypes)
            {
                if (type.IsPublic && type.IsClass && !type.IsAbstract)
                {
                    var interfaces = type.GetInterface(eventHandlerName);
                    if (interfaces is not null)
                    {
                        var @event = interfaces.GenericTypeArguments[0];
                        // Handler 从 Scope 内解析，Scoped 与 DatabaseContext 生命周期一致
                        configuration.Add(ServiceDescriptor.Scoped(typeof(IEventHandler<>).MakeGenericType(@event), type));
                    }
                }
            }

            #endregion
            configuration.Add(ServiceDescriptor.Singleton<IEventBus, LocalEventBus>());
            configuration.Add(ServiceDescriptor.Singleton<IHostedService, EventBusHostService>());
            return configuration;

        }
    }
}
