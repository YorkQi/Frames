using Frame.Core;
using Frame.Repository.Mysql;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TestUnit
{
    public class FrameRepositoryMysqlTest
    {
        private readonly string connectionStr = "数据库连接串";
        [Fact]
        public void MysqlConnectionStr_Test()
        {
            string connectionStr = this.connectionStr;
            IEnumerable<string> connectionStrs = new List<string> { connectionStr, connectionStr };
            MysqlConnectionStr str = new()
            {
                connectionStr
            };
            str.AddRange(connectionStrs);
            var result = str.Get();
            Assert.True(result.Count() == 3);
        }

        [Fact]
        public void MysqlConnectionBuilder_Test()
        {
            //IServiceCollection services = new ServiceCollection();
            //IEnumerable<string> connectionStrs = new List<string> { connectionStr };
            //services.AddMysql(connectionStrs);

            //var provider = services.BuildServiceProvider();

            //IMysqlContext? mysqlContext = services.Servi .builder .ApplicationServices.GetService<IMysqlContext>();
            //MysqlConnectionStr? connectionStrs = app.ApplicationServices.GetService<MysqlConnectionStr>();
            //if (mysqlContext is null) throw new ApplicationException("未找到MySQLContext");
            //if (connectionStrs is null) throw new ApplicationException("未找到MysqlConnectionStr连接池");
            //mysqlContext?.Initialize(RandomConnectionStr(connectionStrs));


            IMysqlConnectionBuilder mysqlConnectionBuilder = new MySqlConnectorBuilder();
            var mysqlConnection = mysqlConnectionBuilder.GetDbConnection(connectionStr);
            var mysqlConnectionAsync = mysqlConnectionBuilder.GetDbConnectionAsync(connectionStr).GetAwaiter().GetResult();
            mysqlConnection.Open();
            mysqlConnection.Close();
            mysqlConnectionAsync.Open();
            mysqlConnectionAsync.Close();
        }


        [Fact]
        public void MysqlCommand_QueryFirst()
        {
            IMysqlConnectionBuilder mysqlConnectionBuilder = new MySqlConnectorBuilder();
            IDBContext mysqlContext = new MysqlDapperContext(mysqlConnectionBuilder);
            mysqlContext.Initialize(connectionStr);
            var name = mysqlContext.QueryFirst<string>("select username from account");
            Equals(name == "york");
        }

        [Fact]
        public void MysqlCommand_Query()
        {
            IMysqlConnectionBuilder mysqlConnectionBuilder = new MySqlConnectorBuilder();
            IDBContext mysqlContext = new MysqlDapperContext(mysqlConnectionBuilder);
            mysqlContext.Initialize(connectionStr);
            var names = mysqlContext.Query<string>("select username from account");
            Assert.True(names.Any());
        }

        [Fact]
        public void MysqlCommand_TranRollback()
        {
            IMysqlConnectionBuilder mysqlConnectionBuilder = new MySqlConnectorBuilder();
            IDBContext mysqlContext = new MysqlDapperContext(mysqlConnectionBuilder);
            mysqlContext.Initialize(connectionStr);
            mysqlContext.BeginTransaction();
            mysqlContext.ExecuteScalar<int>($"insert into account(openid,username,sex,avatar,date_joined)value('{new Guid().ToString().Replace("-", "")}','york2',1,1,'2022-06-15 09:04:18');");
            mysqlContext.Rollback();
        }

        [Fact]
        public void MysqlCommand_TranCommit()
        {
            IMysqlConnectionBuilder mysqlConnectionBuilder = new MySqlConnectorBuilder();
            IDBContext mysqlContext = new MysqlDapperContext(mysqlConnectionBuilder);
            mysqlContext.Initialize(connectionStr);
            mysqlContext.BeginTransaction();
            mysqlContext.ExecuteScalar<int>($"insert into account(openid,username,sex,avatar,date_joined)value('{Guid.NewGuid().ToString().Replace("-", "")}','york2',1,1,'2022-06-15 09:04:18');");
            mysqlContext.Commit();
        }

    }
}