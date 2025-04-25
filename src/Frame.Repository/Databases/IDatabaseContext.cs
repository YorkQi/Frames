using Frame.Repository.Entitys;

namespace Frame.Repository.Databases
{
    public interface IDatabaseContext
    {
        TRepository GetRepository<TRepository>() where TRepository : IRepository;
        IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity;
    }
}
