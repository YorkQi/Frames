using Frame.Core;
using Frame.Repository.DBContexts;
using Frame.Repository.Mysql;
using Frame.Repository.Mysql.ConnectionBuilder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryMysqlDependencyInjection
    {
        public static FrameConfiguration UseMysql(this FrameConfiguration configuration)
        {
            configuration.Add(new ServiceDescriptor(typeof(IDBConnectionBuilder), typeof(MySqlConnectorBuilder), ServiceLifetime.Singleton));
            configuration.Add(new ServiceDescriptor(typeof(IDBContext), typeof(MysqlContext), ServiceLifetime.Scoped));
            return configuration;
        }
    }
}
