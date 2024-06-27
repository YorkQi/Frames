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
            AddLocalCacheEvent<TModule>(services);
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void AddLocalCacheEvent<TModule>(IServiceCollection services) where TModule : IModule
        {
            List<Type> types = new();
            Assembly assembly = Assembly.GetAssembly(typeof(TModule)) ?? throw new ArgumentNullException(nameof(TModule));
            var assemblyTypes = assembly.GetExportedTypes();
            EventHandlerCollection eventHandlerCollection = new();
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
                                eventHandlerCollection.Add(@event, assemblyType);
                            }
                        }
                    }
                }
            }
            var eventHandlers = eventHandlerCollection.Select(t => t.EnventHandlerType);
            foreach (var eventHandler in eventHandlers)
            {
                if (eventHandler.IsPublic && !eventHandler.IsInterface && (eventHandler.IsClass || eventHandler.IsAbstract))
                {
                    services.AddSingleton(eventHandler);
                }
            }
            services.AddSingleton(eventHandlerCollection);
            services.AddSingleton<IEventBus, LocalEventBus>();
            services.AddHostedService<EventBusHostService>();
        }
    }
}
