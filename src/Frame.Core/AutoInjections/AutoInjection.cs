using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Frame.Core
{
    public static class AutoInjection
    {
        /// <summary>
        /// 注入类
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void Injection(this IServiceCollection services, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        var attrs = type.GetCustomAttributes();
                        foreach (var attr in attrs)
                        {
                            if (attr is AutoInjectionAttribute autoInjection)
                            {
                                switch (autoInjection.Lifetime)
                                {
                                    case ServiceLifetime.Transient:
                                        services.AddTransient(type);
                                        break;
                                    case ServiceLifetime.Scoped:
                                        services.AddScoped(type);
                                        break;
                                    case ServiceLifetime.Singleton:
                                        services.AddSingleton(type);
                                        break;
                                    default:
                                        break;
                                }

                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Singleton自动注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void InjectionSingleton(this IServiceCollection services, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        var imps = type.GetInterfaces();
                        if (imps.Any())
                        {
                            var imp = imps.First();
                            services.AddSingleton(imp, type);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Scoped自动注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void InjectionScoped(this IServiceCollection services, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        var imps = type.GetInterfaces();
                        if (imps.Any())
                        {
                            var imp = imps.First();
                            services.AddScoped(imp, type);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Transient自动注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void InjectionTransient(this IServiceCollection services, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        var imps = type.GetInterfaces();
                        if (imps.Any())
                        {
                            var imp = imps.First();
                            services.AddTransient(imp, type);
                        }
                    }
                }
            }
        }
    }
}
