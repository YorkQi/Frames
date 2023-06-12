using Frame.Repository.Context;
using Frame.Repository.DataObjects;
using Frame.Repository.Mysql;
using Frame.Repository.Mysql.DataObjectModel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryMysqlDependencyInjection
    {
        public static IServiceCollection AddMysql(this IServiceCollection services)
        {
            services.AddSingleton<IDataObjectSqlHandler, DataObjectSqlHandler>();
            services.AddSingleton<IDBConnectionBuilder, MySqlConnectorBuilder>();
            services.AddScoped<IDBContext, MysqlDapperContext>();
            return services;
        }
    }
}
