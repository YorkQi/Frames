using System.Collections.Generic;
using System.Threading.Tasks;
using Frame.Core.Application;

namespace Domain.Users
{
    public interface IUserService : IApplicationService
    {
        Task<IEnumerable<User>> GetAsync();
    }
}
