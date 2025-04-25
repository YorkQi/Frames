using Frame.Core.AutoInjections;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Frame.Core
{
    public static class FrameInjectionExtension
    {
        #region 注入

        /// <summary>
        /// Singleton注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void AddSingleton(this IServiceCollection services, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        services.AddSingleton(type);
                    }
                }
            }
        }
        /// <summary>
        /// Singleton注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void AddSingleton(this IServiceCollection services, FrameInjectionCollection collections)
        {
            if (collections is not null)
            {
                foreach (var collection in collections)
                {
                    if (collection.Type.IsPublic && !collection.Type.IsInterface && (collection.Type.IsClass || collection.Type.IsAbstract))
                    {
                        services.AddSingleton(collection.InterfaceType, collection.Type);
                    }
                }
            }
        }

        /// <summary>
        /// Scoped注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void AddScoped(this IServiceCollection services, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        services.AddScoped(type);
                    }
                }
            }
        }

        /// <summary>
        /// Scoped注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void AddScoped(this IServiceCollection services, FrameInjectionCollection collections)
        {
            if (collections is not null)
            {
                foreach (var collection in collections)
                {
                    if (collection.Type.IsPublic && !collection.Type.IsInterface && (collection.Type.IsClass || collection.Type.IsAbstract))
                    {
                        services.AddScoped(collection.InterfaceType, collection.Type);
                    }
                }
            }
        }


        /// <summary>
        /// Transient注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void AddTransient(this IServiceCollection services, FrameInjectionCollection collections)
        {
            if (collections is not null)
            {
                foreach (var collection in collections)
                {
                    if (collection.Type.IsPublic && !collection.Type.IsInterface && (collection.Type.IsClass || collection.Type.IsAbstract))
                    {
                        services.AddTransient(collection.InterfaceType, collection.Type);
                    }
                }
            }
        }

        /// <summary>
        /// Transient注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void AddTransient(this IServiceCollection services, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        services.AddTransient(type);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 注入类
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types"></param>
        public static void AutoInjection(this IServiceCollection services, params Type[] types)
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
                            if (attr is FrameInjectionAttribute autoInjection)
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

    }
}
