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
            configuration.Add(ServiceDescriptor.Singleton<IDBConnectionBuilder, MySqlConnectorBuilder>());
            configuration.Add(ServiceDescriptor.Scoped<IDBContext, MysqlContext>());
            return configuration;
        }
    }
}
