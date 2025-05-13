using Frame.Core;
using Frame.EventBus;
using Microsoft.Extensions.Hosting;
using System;
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
        public static FrameConfiguration UseEventBus(this FrameConfiguration configuration)
        {
            #region 注入事件操作类
            var assemblyTypes = configuration.GetAssemblyType();
            var eventHandlerName = typeof(IEventHandler<>).FullName ?? throw new ArgumentNullException(typeof(IEventHandler<>).FullName);
            foreach (var type in assemblyTypes.Types)
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
