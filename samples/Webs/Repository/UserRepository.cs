using Domain.Users;
using Frame.Databases.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class UserRepository :
        Repository<int, User>,
        IUserRepository
    {

        public Task<IEnumerable<User>> QueryAsync()
        {
            var sql = "SELECT * FROM `User` WHERE Id > 1 ; ";
            return DBContext.QueryAsync<User>(sql);
        }
    }
}
