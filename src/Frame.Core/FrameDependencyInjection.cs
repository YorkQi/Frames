using Frame.Core;
using Frame.Core.FrameModules;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FrameDependencyInjection
    {
        /// <summary>
        /// 全局注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameService(this IServiceCollection services, Action<FrameConfiguration>? configuration = null)
        {
            var builder = new FrameModuleConfigurationBuilder();
            var config = builder.AddModule().Build();
            configuration += config;

            var frameConfiguration = new FrameConfiguration(GetAssemblyType());
            configuration?.Invoke(frameConfiguration);

            services.Add(frameConfiguration.GetServiceDescriptor());
            return services;
        }


        private static FrameAssemblyType GetAssemblyType()
        {
            FrameAssemblyType assemblyType = new();
            var assemblies = GetAssembliesAll();
            assemblies = [.. assemblies.Where(assembly =>
            assembly.GetTypes().Any(type => type.IsClass && !type.IsAbstract)).Distinct()];
            foreach (var itemAssembly in assemblies)
            {
                var assemblyTypes = itemAssembly.GetExportedTypes();
                if (assemblyTypes.Any())
                {
                    assemblyType.Types.AddRange(assemblyTypes);
                }
            }
            return assemblyType;
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
