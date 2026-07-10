using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Users;
using Frame.Databases.Repositories;

namespace Repository
{
    public class UserRepository :
        Repository<int, User>,
        IUserRepository
    {
        /// <summary>
        /// 通过 IDBContext.QueryAsync 执行原始 SQL 查询
        /// </summary>
        public Task<IEnumerable<User>> QueryAsync()
        {
            var sql = "SELECT * FROM `Users` WHERE Id > 1 ; ";
            return DBContext.QueryAsync<User>(sql);
        }

        /// <summary>
        /// 通过 IDBContext.QueryAsync 执行参数化查询
        /// </summary>
        public Task<IEnumerable<User>> QueryByDapperAsync(string name)
        {
            return DBContext.QueryAsync<User>(
                "SELECT * FROM `Users` WHERE Name LIKE @Name",
                new { Name = $"%{name}%" });
        }
    }
}
