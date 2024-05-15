using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class DependencyInjectionExtentions
    {
        /// <summary>
        /// 注入类
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal static void Injection(this IServiceCollection services, params Type[] types)
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
                                        throw new ApplicationException("Injection自动注入异常");

                                }

                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 注入类
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal static void InjectionService(this IServiceCollection services, params Type[] types)
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
                            var imp = imps.FirstOrDefault() ?? throw new ApplicationException("InjectionService自动注入异常");
                            services.AddSingleton(imp, type);
                        }
                    }
                }
            }
        }
    }
}
