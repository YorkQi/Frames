using Domain.Users;
using Frame.Repository;
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
            return Context.QueryAsync<User>(sql);
        }
    }
}
