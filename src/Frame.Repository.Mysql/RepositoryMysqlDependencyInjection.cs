using Frame.Repository.Context;
using Frame.Repository.Mysql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryMysqlDependencyInjection
    {
        public static IServiceCollection AddMysql(this IServiceCollection services)
        {
            services.AddSingleton<IDBConnectionBuilder, MySqlConnectorBuilder>();
            services.AddScoped<IDBContext, MysqlDapperContext>();
            return services;
        }
    }
}
