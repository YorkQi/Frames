using Xunit;

namespace TestUnit
{
    public class FrameRepositoryTest
    {
        private readonly string connectionStr = "���ݿ����Ӵ�";

        [Fact]
        public void Repository_QueryFirst()
        {
            //IMysqlConnectionBuilder mysqlConnectionBuilder = new MySqlConnectorBuilder();
            //IDBContext mysqlContext = new MysqlDapperContext(mysqlConnectionBuilder);
            //mysqlContext.Initialize(connectionStr);
            //var name = mysqlContext.QueryFirst<string>("select username from account");
            //Equals(name == "york");
        }
    }
}