using Frame.Repository.DBContexts;
using Frame.Repository.Entitys;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Frame.Repository.Databases
{
    /// <summary>
    /// 数据库上下文
    /// </summary>
    public class DatabaseContext : IDatabaseContext
    {
        private IServiceProvider provider = default!;
        private string dbConnectionString = default!;

        internal void Initialize(IServiceProvider provider, DBConnectionString dbConnectionString)
        {
            this.provider = provider;
            this.dbConnectionString = RandomConnectionString(dbConnectionString);
        }

        public TRepository GetRepository<TRepository>() where TRepository : IRepository
        {
            var serviceScope = provider.CreateScope();
            IDBContext dbContext = serviceScope.ServiceProvider.GetService<IDBContext>()
                ?? throw new ArgumentNullException(nameof(IDBContext));
            dbContext.Initialize(dbConnectionString);
            var repository = serviceScope.ServiceProvider.GetService<TRepository>()
                ?? throw new ArgumentNullException(nameof(TRepository));
            repository.Initialize(dbContext);
            return repository;
        }


        public IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity
        {
            var serviceScope = provider.CreateScope();
            IDBContext dbContext = serviceScope.ServiceProvider.GetService<IDBContext>()
                ?? throw new ArgumentNullException(nameof(IDBContext));
            dbContext.Initialize(dbConnectionString);
            var repository = serviceScope.ServiceProvider.GetService<IRepository<TPrimaryKey, TEntity>>()
                ?? throw new ArgumentNullException(nameof(IRepository<TPrimaryKey, TEntity>));
            repository.Initialize(dbContext);
            return repository;
        }


        /// <summary>
        /// 随机取得连接串
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static string RandomConnectionString(DBConnectionString connectionString)
        {
            Random random = new();
            var index = random.Next(0, connectionString.Count() - 1);
            var connectionStr = connectionString.Get();
            return connectionStr.ElementAt(index);
        }
    }
}
