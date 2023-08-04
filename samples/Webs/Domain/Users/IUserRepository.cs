using Domain.Users;
using Frame.Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public interface IUserRepository :
        IRepository<int, User>
    {
        Task<IEnumerable<User>> QueryAsync();
    }
}
