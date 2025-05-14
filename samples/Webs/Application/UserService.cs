using Domain.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application
{
    public class UserService : IUserService
    {
        public async Task<IEnumerable<User>> LoginAsync()
        {
            return await Task.Run(() =>
            {
                return new List<User>();
            });
        }
    }
}
