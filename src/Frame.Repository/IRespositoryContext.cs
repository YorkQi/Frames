using Frame.Core.Entitys;

namespace Frame.Repository
{
    /// <summary>
    /// 仓储工厂
    /// </summary>
    public interface IRespositoryContext
    {
        /// <summary>
        /// 返回仓储实体
        /// </summary>
        /// <typeparam name="TRepository"></typeparam>
        /// <returns></returns>
        TRepository Get<TRepository>() where TRepository : IRepository;

        /// <summary>
        /// 返回仓储实体
        /// </summary>
        /// <returns></returns>
        IRepository<TPrimaryKey, TEntity> Get<TPrimaryKey, TEntity>() where TEntity : class, IEntity;
    }
}
