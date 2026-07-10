namespace Frame.Databases.DbContexts
{
    /// <summary>
    /// Scoped 连接字符串持有者。
    /// DatabaseContext.GetRepository 写入，EfCoreContext 构造器读取，
    /// 替代 IDBContext.Initialize 的两步初始化。
    /// </summary>
    public class DbContextConnectionStringAccessor
    {
        public string? ConnectionString { get; set; }
    }
}
