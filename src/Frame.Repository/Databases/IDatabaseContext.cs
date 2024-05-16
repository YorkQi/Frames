using Frame.Core.Entitys;

namespace Frame.Repository.Databases
{
    /// <summary>
    /// 数据库上下文
    /// </summary>
    public interface IDatabaseContext
    {
        /// <summary>
        /// 返回仓储实体
        /// </summary>
        /// <typeparam name="TRepository"></typeparam>
        /// <returns></returns>
        TRepository GetRepository<TRepository>() where TRepository : IRepository;

        /// <summary>
        /// 返回仓储实体
        /// </summary>
        /// <returns></returns>
        IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity;
    }
}
