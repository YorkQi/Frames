using Frame.Core;
using Frame.Databases.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Databases
{
    public static class DatabaseContextConfiguration
    {
        public static void UseDatabaseContext<TDatabaseContext>(
            [NotNull] this ServiceConfigurationContext configuration,
            [NotNull] DatabaseConnections dbconnections)
            where TDatabaseContext : DatabaseContext, new()
        {
            Check.NotNull(configuration, nameof(configuration));
            Check.NotNull(dbconnections, nameof(dbconnections));
            configuration.Add(ServiceDescriptor.Scoped((provider) =>
            {
                var databaseContext = new TDatabaseContext();
                databaseContext.Initialize(provider, dbconnections);
                return databaseContext;
            }));

            UseRepository(configuration);

        }


        private static void UseRepository([NotNull] ServiceConfigurationContext configuration)
        {
            Check.NotNull(configuration, nameof(configuration));
            var assemblyTypes = configuration.GetAssemblyType();
            foreach (var type in assemblyTypes)
            {
                if (type.IsClass && type.GetInterface(nameof(IRepository)) is not null)
                {
                    var interfaceTypes = type.GetInterfaces();//取得仓储类继承的所有接口
                    foreach (var interfaceType in interfaceTypes)
                    {
                        var imps = interfaceType.GetInterfaces();//取得仓储类继承的接口网上继承的接口
                        foreach (var imp in imps)
                        {
                            //如果往上继承的接口泛型接口并且继承IRepository接口则就是想要的接口
                            if (imp.IsGenericType && imp.GetInterface(nameof(IRepository)) is not null)
                            {
                                configuration.Add(ServiceDescriptor.Scoped(interfaceType, type));
                                configuration.Add(ServiceDescriptor.Scoped(imp, type));
                            }
                        }
                    }
                }
            }
        }
    }
}
