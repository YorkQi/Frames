using System.Diagnostics.CodeAnalysis;
using Frame.Core;
using Frame.Core.Repositories;
using Frame.Core.Utils;
using Frame.Databases.DBContext;
using Frame.Databases.DbContexts;
using Frame.Databases.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Databases
{
    public static class DatabaseContextConfiguration
    {
        public static void UseDatabaseContext<TDatabaseContext>(
            [NotNull] this ServiceConfigurationContext configuration,
            [NotNull] ConnectionStringCollection dbconnections,
            ConnectionStringStrategy strategy = ConnectionStringStrategy.Random)
            where TDatabaseContext : DatabaseContext, new()
        {
            Check.NotNull(configuration, nameof(configuration));
            Check.NotNull(dbconnections, nameof(dbconnections));

            configuration.Add(ServiceDescriptor.Scoped<DbContextConnectionStringAccessor, DbContextConnectionStringAccessor>());

            configuration.Add(ServiceDescriptor.Scoped<IDbContext, DbContext>());

            configuration.Add(ServiceDescriptor.Scoped((provider) =>
            {
                var databaseContext = new TDatabaseContext();
                databaseContext.Initialize(provider, dbconnections, strategy);
                return databaseContext;
            }));

            UseRepositories(configuration);
        }


        private static void UseRepositories([NotNull] ServiceConfigurationContext configuration)
        {
            Check.NotNull(configuration, nameof(configuration));
            var assemblyTypes = configuration.GetAssemblyType();
            foreach (var type in assemblyTypes)
            {
                // 跳过抽象类、开放泛型、未实现 IRepository 的类
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters
                    || type.GetInterface(nameof(IRepository)) is null)
                    continue;

                // 遍历类实现的所有接口，把继承自 IRepository 的（除标记接口自身）都注册
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface != typeof(IRepository) && iface.GetInterface(nameof(IRepository)) is not null)
                    {
                        configuration.Add(ServiceDescriptor.Scoped(iface, type));
                    }
                }
            }
        }
    }
}
