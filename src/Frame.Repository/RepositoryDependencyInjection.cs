using Frame.Core.DependencyInjection;
using Frame.Repository;
using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryDependencyInjection
    {
        public static IServiceCollection AddRepository<TModule>(this IServiceCollection services, Action<RepositoryBuilder> configBuilder) where TModule : class, IModule
        {
            RepositoryBuilder repositoryConfiguration = new RepositoryBuilder();
            configBuilder?.Invoke(repositoryConfiguration);
            var configs = repositoryConfiguration.Get();
            foreach (var config in configs)
            {
                if (config.TypeKey != null && config.TypeValue != null)
                {
                    services.AddSingleton(config.TypeKey, config.TypeValue);
                }
            }

            #region 将所有仓储类单例注入
            var assembly = Assembly.GetAssembly(typeof(TModule));
            if (assembly is null) throw new ApplicationException("未找到程序集无法注入");
            var exportedTypes = assembly.GetExportedTypes();
            foreach (var classType in exportedTypes)
            {
                if (classType.IsClass)
                {
                    var interfaceTypes = classType.GetInterfaces();//取得仓储类继承的所有接口
                    foreach (var interfaceType in interfaceTypes)
                    {
                        var imps = interfaceType.GetInterfaces();//取得仓储类继承的接口网上继承的接口
                        foreach (var imp in imps)
                        {
                            if (imp.IsGenericType && imp.GetInterface(nameof(IRepository)) != null)//如果往上继承的接口泛型接口并且继承IRepository接口则就是想要的接口
                            {
                                services.AddScoped(interfaceType, classType);
                            }
                        }
                    };
                }
            }

            #endregion


            return services;
        }
    }
}
