using Frame.Repository.DBContexts;
using MySqlConnector;
using System.Data;
using System.Threading.Tasks;

namespace Frame.Repository.Mysql
{
    public class MySqlConnectorBuilder : IDBConnectionBuilder
    {

        public MySqlConnectorBuilder()
        {
        }

        public IDbConnection GetDbConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        public async Task<IDbConnection> GetDbConnectionAsync(string connectionString)
        {
            return await Task.Run(() => { return new MySqlConnection(connectionString); });
        }
    }
}
