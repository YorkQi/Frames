using System.Data;
using System.Threading.Tasks;

namespace Frame.Repository.Context
{
    /// <summary>
    /// 构建连接对象实例接口
    /// </summary>
    public interface IDBConnectionBuilder
    {
        IDbConnection GetDbConnection(string connectionString);

        Task<IDbConnection> GetDbConnectionAsync(string connectionString);
    }
}
