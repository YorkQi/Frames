using Frame.EventBus;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventDependencyInjection
    {
        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEvent(this IServiceCollection services, EventMode mode = EventMode.LocalCache)
        {
            switch (mode)
            {
                case EventMode.LocalCache:
                    AddLocalCacheEvent(services);
                    break;
                case EventMode.Redis:
                    AddRedisEvent(services);
                    break;
                case EventMode.RabbitMQ:
                    AddRabbitMQEvent(services);
                    break;
                default:
                    throw new ApplicationException("事件模式错误");
            }
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AddLocalCacheEvent(IServiceCollection services)
        {
            services.AddSingleton<IEventFactory, LocalCacheEventFactory>();
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
