using Domain.Users;
using Infrastructure.DatabaseContexts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application
{
    public class UserService : IUserService
    {
        private readonly CommandDatabaseContext command;
        public UserService(CommandDatabaseContext command)
        {
            this.command = command;
        }
        public async Task<IEnumerable<User>> LoginAsync()
        {
            return new List<User>();
        }
    }
}
