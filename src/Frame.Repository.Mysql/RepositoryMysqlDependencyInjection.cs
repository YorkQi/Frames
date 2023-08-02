using Frame.Repository.Context;
using Frame.Repository.Mysql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryMysqlDependencyInjection
    {
        public static IServiceCollection AddMysql(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionBuilder, MySqlConnectorBuilder>();
            services.AddScoped<IContext, MysqlDapperContext>();
            return services;
        }
    }
}
