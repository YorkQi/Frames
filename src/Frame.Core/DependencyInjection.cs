﻿using Frame.Core.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFrameCore(this IServiceCollection services)
        {
            services.AddModule();
            services.AddApplication();
            return services;
        }

        internal static IServiceCollection AddApplication(this IServiceCollection services)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            Assembly[] assemblies = currentDomain.GetAssemblies();//取得所有程序集
            List<Type> types = new();                                                 
            foreach (var itemAssembly in assemblies)
            {
                foreach (var type in itemAssembly.GetExportedTypes())
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        var imps = type.GetInterfaces();
                        if (imps.Any(t => t.Equals(typeof(IApplication))))//取得所有继承IAppcation的类
                        {
                            types.Add(type);
                        }
                    }
                }
            }
            services.InjectionService(types.ToArray());
            return services;
        }

        internal static IServiceCollection AddModule(this IServiceCollection services)
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? throw new ApplicationException("assembly加载异常");
            List<Type> types = new();
            types.AddRange(assembly.GetExportedTypes() ?? throw new ApplicationException("types加载异常"));
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach (var item in referencedAssemblies)
            {
                Assembly itemAssembly = Assembly.Load(item);
                types.AddRange(itemAssembly.GetExportedTypes());
            }
            services.Injection(types.ToArray());
            return services;
        }

        internal static IServiceCollection AddModule<TModule>(this IServiceCollection services) where TModule : class
        {
            Assembly assembly = Assembly.GetAssembly(typeof(TModule)) ?? throw new ApplicationException("assembly加载异常");
            var types = assembly.GetExportedTypes();
            services.Injection(types);
            return services;
        }
    }
}
