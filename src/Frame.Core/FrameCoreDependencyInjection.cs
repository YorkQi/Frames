using Frame.Core;
using Frame.Core.AutoInjections;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FrameCoreDependencyInjection
    {
        /// <summary>
        /// 全局注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameCore(this IServiceCollection services)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();//取得所有程序集
            List<Type> types = new();
            foreach (var itemAssembly in assemblies)
            {
                types.AddRange(itemAssembly.GetExportedTypes());
            }
            services.AutoInjection(types.ToArray());
            return services;
        }

        /// <summary>
        /// 依赖IModule注入
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddFrameCore<TModule>(this IServiceCollection services) where TModule : IModule
        {
            Type typeFromHandle = typeof(TModule);
            Assembly assembly = typeFromHandle.Assembly;
            if (string.IsNullOrWhiteSpace(typeFromHandle.FullName)) throw new ArgumentNullException(nameof(TModule));
            var moduleObj = assembly.CreateInstance(typeFromHandle.FullName) ?? throw new ArgumentNullException(nameof(TModule));
            var moudule = (IModule)moduleObj;
            moudule.Load(services);

            var types = assembly.GetExportedTypes();
            services.AutoInjection(types);
            return services;
        }
    }
}
