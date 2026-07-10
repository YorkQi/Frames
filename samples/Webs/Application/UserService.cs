using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Users;
using Infrastructure.DatabaseContexts;

namespace Application
{
    public class UserService(CommandDatabaseContext command) : IUserService
    {
        public async Task<IEnumerable<User>> GetAsync()
        {
            var repo = command.GetRepository<IUserRepository>();
            return await repo.QueryAllAsync();
        }
    }
}
