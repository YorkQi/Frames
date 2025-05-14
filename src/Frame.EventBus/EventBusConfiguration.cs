using Frame.Core;
using Frame.EventBus;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


namespace Microsoft.Extensions.DependencyInjection
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
            Check.NotNull(eventHandlerName, nameof(eventHandlerName));

            #region 注入事件操作类
            var assemblyTypes = configuration.GetAssemblyType();
            foreach (var type in assemblyTypes)
            {
                if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                {
                    var interfaces = type.GetInterface(eventHandlerName);
                    if (interfaces is not null)
                    {
                        var @event = interfaces.GenericTypeArguments.FirstOrDefault();
                        if (@event is not null)
                        {
                            configuration.Add(ServiceDescriptor.Singleton(typeof(IEventHandler<>).MakeGenericType(@event), type));
                        }
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
