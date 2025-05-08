using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Frame.Core.FrameModules
{
    public static class FrameModuleDependencyInjection
    {

        /// <summary>
        /// 全局注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameModule(this IServiceCollection services)
        {
            var assemblies = GetAssembliesAll();
            assemblies = [.. assemblies.Where(assembly =>
            assembly.GetTypes().Any(type => type.IsClass && !type.IsAbstract)).Distinct()];
            FrameServiceDescriptor collections = [];
            foreach (var itemAssembly in assemblies)
            {
                var assemblyTypes = itemAssembly.GetExportedTypes();
                if (assemblyTypes.Any())
                {
                    collections.AddFrameServiceDescriptor(assemblyTypes);
                    collections.AddApplication(assemblyTypes);
                }
            }
            services.Add([.. collections]);
            return services;
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
