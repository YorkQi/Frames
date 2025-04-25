using Frame.Core.Application;
using Frame.Core.AutoInjections;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationDependencyInjection
    {
        public static IServiceCollection AddApplication<TModule>(this IServiceCollection services) where TModule : IModule
        {
            FrameInjectionCollection collections = new();
            Assembly assembly = Assembly.GetAssembly(typeof(TModule)) ?? throw new ArgumentNullException(nameof(TModule));
            var assemblyTypes = assembly.GetExportedTypes();

            foreach (var assemblyType in assemblyTypes)
            {
                if (assemblyType.IsPublic && !assemblyType.IsInterface && (assemblyType.IsClass || assemblyType.IsAbstract))
                {
                    var imps = assemblyType.GetInterfaces();
                    if (imps.Any(t => t.Equals(typeof(IApplication))))//取得所有继承IAppcation的类
                    {
                        var interfaceType = imps.First();
                        collections.Add(assemblyType, interfaceType);
                        services.AddSingleton(interfaceType, assemblyType);
                    }
                }
            }
            services.AddSingleton(collections);
            return services;
        }
    }
}
