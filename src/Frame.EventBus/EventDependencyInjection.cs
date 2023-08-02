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
        public static IServiceCollection AddEvent(this IServiceCollection services, EventType mode = EventType.LocalCache)
        {
            switch (mode)
            {
                case EventType.LocalCache:
                    AddLocalCacheEvent(services);
                    break;
                case EventType.Redis:
                    AddRedisEvent(services);
                    break;
                case EventType.RabbitMQ:
                    AddRabbitMQEvent(services);
                    break;
                default:
                    throw new ApplicationException("Event类型错误");
            }
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AddLocalCacheEvent(IServiceCollection services)
        {
            EventHandlerCollection eventHandlerCollection = new EventHandlerCollection();
            Assembly assembly = Assembly.GetEntryAssembly();
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                var interfaces = type.GetInterface(typeof(IEventHandler<>).FullName);
                if (interfaces != null)
                {
                    var @event = interfaces.GenericTypeArguments.FirstOrDefault();
                    eventHandlerCollection.Add(@event, type);
                    services.AddScoped(type);
                }
            }
            services.AddSingleton<IEventBus>(p =>
            {
                var eventBus = new LocalCacheEventBus(services.BuildServiceProvider());
                eventBus.Init(eventHandlerCollection);
                eventBus.StartExec();
                return eventBus;
            });
        }
        /// <summary>
        /// 
        /// </summary>
        private static void AddRedisEvent(IServiceCollection services)
        {
            //services.AddSingleton<IEventFactory, RedisEventFactory>();
        }
        /// <summary>
        /// 
        /// </summary>
        private static void AddRabbitMQEvent(IServiceCollection services)
        {
            //services.AddSingleton<IEventFactory, RabbitMQEventFactory>();
        }
    }
}
