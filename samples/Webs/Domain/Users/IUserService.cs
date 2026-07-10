using System.Collections.Generic;
using System.Threading.Tasks;
using Frame.Core.Application;
using Frame.Core.Entities.Dtos;

namespace Domain.Users
{
    public interface IUserService : IApplicationService
    {
        Task<IEnumerable<User>> GetAsync();

        /// <summary>分页查询用户</summary>
        Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size);
    }
}
