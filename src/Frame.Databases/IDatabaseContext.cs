using Frame.Databases.Entitys;
using Frame.Databases.Repositories;

namespace Frame.Databases
{
    public interface IDatabaseContext
    {
        TRepository GetRepository<TRepository>() where TRepository : IRepository;
        IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>() where TEntity : class, IEntity;
    }
}
