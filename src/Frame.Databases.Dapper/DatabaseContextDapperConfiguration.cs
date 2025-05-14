using Frame.Core;
using Frame.Databases.Dapper.ConnectionBuilder;
using Frame.Databases.Dapper.Dapper;
using Frame.Databases.DBContexts;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseContextDapperConfiguration
    {
        public static ServiceConfigurationContext UseDapperMysql([NotNull] this ServiceConfigurationContext configuration)
        {
            Check.NotNull(configuration, nameof(configuration));

            configuration.Add(ServiceDescriptor.Singleton<IDBConnectionBuilder, MySqlConnectorBuilder>());
            configuration.Add(ServiceDescriptor.Scoped<IDBContext, MysqlContext>());
            return configuration;
        }
    }
}
