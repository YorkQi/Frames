using Frame.Core;
using Frame.Databases.DBContexts;
using Frame.Databases.Entitys;
using Frame.Databases.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Frame.Databases
{
    /// <summary>
    /// 数据库上下文
    /// </summary>
    public class DatabaseContext : IDatabaseContext
    {
        [NotNull]
        private IServiceProvider provider = default!;
        [NotNull]
        private string dbConnectionString = default!;

        internal void Initialize(IServiceProvider provider, DatabaseConnections dbConnectionString)
        {
            this.provider = provider;
            this.dbConnectionString = dbConnectionString.ToArray().RandomElement();
        }

        public TRepository GetRepository<TRepository>() where TRepository : IRepository
        {
            Check.NotNull(provider, nameof(provider));

            var serviceScope = provider.CreateScope();
            IDBContext dbContext = serviceScope.ServiceProvider.GetRequiredService<IDBContext>();
            dbContext.Initialize(dbConnectionString);

            var repository = serviceScope.ServiceProvider.GetRequiredService<TRepository>();
            repository.Initialize(dbContext);
            return repository;
        }


        public IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity
        {
            Check.NotNull(provider, nameof(provider));

            var serviceScope = provider.CreateScope();
            IDBContext dbContext = serviceScope.ServiceProvider.GetRequiredService<IDBContext>();
            dbContext.Initialize(dbConnectionString);

            var repository = serviceScope.ServiceProvider.GetRequiredService<IRepository<TPrimaryKey, TEntity>>();
            repository.Initialize(dbContext);
            return repository;
        }
    }
}
