using Frame.Core.Application;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Users
{
    public interface IUserService : IApplicationService
    {
        Task<IEnumerable<User>> LoginAsync();
    }
}
