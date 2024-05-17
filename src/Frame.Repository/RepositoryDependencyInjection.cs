using Frame.Core.AutoInjections;
using Frame.Repository;
using Frame.Repository.Databases;
using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryDependencyInjection
    {
        public static IServiceCollection AddRepository<TModule>(this IServiceCollection services,
            Action<DatabaseContextBuilder> databaseContextBuilder) where TModule : class, IModule
        {
            #region 注入DataBaseContext数据库上下文

            DatabaseContextBuilder builder = new();
            databaseContextBuilder?.Invoke(builder);
            var databaseContexts = builder.GetDatabaseContext();
            foreach (var databaseContext in databaseContexts)
            {
                if (databaseContext.DataBaseContextType is not null && databaseContext.DataBaseContextProvider is not null)
                {
                    services.AddSingleton(databaseContext.DataBaseContextType, databaseContext.DataBaseContextProvider);
                }
            }

            #endregion

            #region 将注入仓储类
            var assembly = Assembly.GetAssembly(typeof(TModule)) ?? throw new ArgumentNullException(nameof(TModule));
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
