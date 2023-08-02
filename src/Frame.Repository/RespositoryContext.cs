using Frame.Core.Entitys;
using Frame.Repository.Context;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Frame.Repository
{
    /// <summary>
    /// 仓储上下文（实现仓储的数据库对象实例化）
    /// 项目业务不在对应数据库实体  直接对应仓储
    /// </summary>
    public class RespositoryContext : IRespositoryContext
    {
        private IServiceProvider? service;
        /// <summary>
        /// 连接串
        /// </summary>
        private ConnectionStr? ConnectionStr { get; set; }


        internal void Initialize(IServiceProvider service, ConnectionStr connectionStr)
        {
            this.service = service;
            ConnectionStr = connectionStr;
        }

        public TRepository Get<TRepository>() where TRepository : IRepository
        {
            if (service is null) throw new ApplicationException("取得仓储异常");
            var serviceScope = service.CreateScope();
            var mysqlContext = serviceScope.ServiceProvider.GetService<IContext>() ?? throw new ApplicationException("取得仓储异常");
            if (ConnectionStr is null) throw new ApplicationException("未设置仓储连接串");
            mysqlContext.Initialize(RandomConnectionStr(ConnectionStr));
            var repsitory = serviceScope.ServiceProvider.GetService<TRepository>() ?? throw new ApplicationException("取得仓储接口异常");
            repsitory.Context = mysqlContext ?? throw new ApplicationException("DBContext异常");
            return repsitory;
        }


        public IRepository<TPrimaryKey, TEntity> Get<TPrimaryKey, TEntity>() where TEntity : class, IEntity
        {
            if (service is null) throw new ApplicationException("取得仓储异常");
            var serviceScope = service.CreateScope();
            var mysqlContext = serviceScope.ServiceProvider.GetService<IContext>() ?? throw new ApplicationException("取得仓储异常");
            if (ConnectionStr is null) throw new ApplicationException("未设置仓储连接串");
            mysqlContext.Initialize(RandomConnectionStr(ConnectionStr));

            var repository = new Repository<TPrimaryKey, TEntity>()
            {
                Context = mysqlContext ?? throw new ApplicationException("DBContext异常")
            };
            return repository;
        }


        /// <summary>
        /// 随机取得连接串
        /// </summary>
        /// <param name="connectionStrs"></param>
        /// <returns></returns>
        private static string RandomConnectionStr(ConnectionStr connectionStrs)
        {
            if (!connectionStrs.Any()) throw new ApplicationException("未设置仓储连接串");
            Random random = new Random();
            var index = random.Next(0, (connectionStrs.Count() - 1));
            var connectionStr = connectionStrs.Get();
            return connectionStr.ElementAt(index);
        }
    }
}
