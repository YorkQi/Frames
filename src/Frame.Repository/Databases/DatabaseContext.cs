using Frame.Core.Entitys;
using Frame.Repository.DBContexts;
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
        private IServiceProvider? provider;
        private DBConnectionString? dbConnectionStr;

        internal void Initialize(IServiceProvider provider, DBConnectionString dbConnectionStr)
        {
            this.provider = provider;
            this.dbConnectionStr = dbConnectionStr;
        }

        public TRepository GetRepository<TRepository>() where TRepository : IRepository
        {
            var serviceScope = provider?.CreateScope();
            IDBContext mysqlContext = serviceScope?.ServiceProvider.GetService<IDBContext>() ?? throw new ArgumentNullException(nameof(IDBContext));
            mysqlContext.Initialize(RandomConnectionStr(dbConnectionStr));
            var repsitory = serviceScope.ServiceProvider.GetService<TRepository>() ?? throw new ArgumentNullException(nameof(TRepository));
            repsitory.DBContext = mysqlContext;
            return repsitory;
        }


        public IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity
        {
            var serviceScope = provider?.CreateScope();
            IDBContext mysqlContext = serviceScope?.ServiceProvider.GetService<IDBContext>() ?? throw new ArgumentNullException(nameof(IDBContext));
            mysqlContext.Initialize(RandomConnectionStr(dbConnectionStr));
            var repository = new Repository<TPrimaryKey, TEntity>()
            {
                DBContext = mysqlContext
            };
            return repository;
        }


        /// <summary>
        /// 随机取得连接串
        /// </summary>
        /// <param name="connectionStrs"></param>
        /// <returns></returns>
        private static string RandomConnectionStr(DBConnectionString? connectionStrs)
        {
            if (connectionStrs is null || !connectionStrs.Any())
            {
                throw new ArgumentNullException(nameof(DBConnectionString));
            }
            Random random = new();
            var index = random.Next(0, connectionStrs.Count() - 1);
            var connectionStr = connectionStrs.Get();
            return connectionStr.ElementAt(index);
        }
    }
}
