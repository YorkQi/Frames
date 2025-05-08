using Frame.Core;
using Frame.EventBus;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventBusConfiguration
    {
        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static FrameConfiguration UseEventBus(this FrameConfiguration configuration)
        {
            #region 注入事件操作类

            var assemblies = GetAssembliesAll();
            var eventHandlerName = typeof(IEventHandler<>).FullName;
            foreach (var assembly in assemblies)
            {
                var assemblyTypes = assembly.GetExportedTypes();
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
                                    configuration.Add(new ServiceDescriptor(typeof(IEventHandler<>).MakeGenericType(@event), assemblyType, ServiceLifetime.Singleton));
                                }
                            }
                        }
                    }
                }
            }

            #endregion
            configuration.Add(new ServiceDescriptor(typeof(IEventBus), typeof(LocalEventBus), ServiceLifetime.Singleton));
            configuration.Add(new ServiceDescriptor(typeof(IHostedService), typeof(EventBusHostService), ServiceLifetime.Singleton));
            return configuration;
        }

        private static IEnumerable<Assembly> GetAssembliesAll()
        {
            var assemblies = new List<Assembly>();
            var basePath = AppContext.BaseDirectory;
            foreach (var dll in Directory.GetFiles(basePath, "*.dll"))
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(dll));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载失败：{dll}，错误：{ex.Message}");
                }
            }
            return assemblies;
        }
    }
}
