using Frame.Repository.DBContexts;

namespace Frame.Repository.Mysql
{
    public partial class MysqlContext : IDBContext
    {
        //private DbContext? EFDbContext { get; set; }

        //public async Task<DbSet<TEntity>> QueryDbSetAsync<TEntity>() where TEntity : class
        //{
        //    return await Task.Run(() => { return EFDbContext?.Set<TEntity>(); });
        //}
    }
}
