using Frame.Core.AutoInjections;
using Frame.EventBus;
using System;
using System.Collections.Generic;
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
        public static IServiceCollection AddEventBus<TModule>(this IServiceCollection services) where TModule : IModule
        {
            #region 注入事件操作类

            List<Type> types = new();
            Assembly assembly = Assembly.GetAssembly(typeof(TModule)) ?? throw new ArgumentNullException(nameof(TModule));
            var assemblyTypes = assembly.GetExportedTypes();
            var eventHandlerName = typeof(IEventHandler<>).FullName;
            if (eventHandlerName is not null)
            {
                foreach (var assemblyType in assemblyTypes)
                {
                    if (assemblyType.IsPublic && !assemblyType.IsInterface && (assemblyType.IsClass || assemblyType.IsAbstract))
                    {
                        var interfaces = assemblyType.GetInterface(eventHandlerName);
                        if (interfaces is not null)
                        {
                            var @event = interfaces.GenericTypeArguments.FirstOrDefault();
                            if (@event is not null)
                            {
                                services.AddSingleton(typeof(IEventHandler<>).MakeGenericType(@event), assemblyType);
                            }
                        }
                    }
                }
            }

            #endregion
            
            AddLocalCacheEvent(services);

            services.AddHostedService<EventBusHostService>();
            return services;
        }

        /// <summary>
        /// 本地缓存
        /// </summary>
        private static void AddLocalCacheEvent(IServiceCollection services)
        {
            services.AddSingleton<IEventBus, LocalEventBus>();
        }
    }
}
