using Frame.Core.AutoInjections;
using Frame.Repository.DBContexts;
using Frame.Repository.Mysql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryMysqlDependencyInjection
    {
        public static IServiceCollection AddMysql<TModule>(this IServiceCollection services) where TModule : class, IModule
        {
            services.AddSingleton<IDBConnectionBuilder, MySqlConnectorBuilder>();
            services.AddScoped<IDBContext, MysqlDapperContext>();
            return services;
        }
    }
}
