
using Frame.EventBus;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventDependencyInjection
    {
        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEvent(this IServiceCollection services)
        {
            AddLocalCacheEvent(services);
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AddLocalCacheEvent(IServiceCollection services)
        {
            EventHandlerCollection eventHandlerCollection = new();
            var eventHandlerName = typeof(IEventHandler<>).FullName;
            if (eventHandlerName is not null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();//取得所有程序集
                foreach (var itemAssembly in assemblies)
                {
                    foreach (var type in itemAssembly.GetExportedTypes())
                    {
                        var interfaces = type.GetInterface(eventHandlerName);
                        if (interfaces is not null)
                        {
                            var @event = interfaces.GenericTypeArguments.FirstOrDefault();
                            if (@event is not null)
                            {
                                eventHandlerCollection.Add(@event, type);
                                services.AddSingleton(type);
                            }
                        }
                    }
                }
            }
            services.AddSingleton(eventHandlerCollection);
            services.AddSingleton<IEventBus, LocalEventBus>();
            services.AddHostedService<EventBusHostService>();
        }
    }
}
