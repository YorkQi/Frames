using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Users;
using Frame.Core.Entities.Dtos;
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

        /// <summary>
        /// 分页查询：通过 IUserRepository.QueryPagedAsync 实现，
        /// 底层使用 DbContext.QueryPagedAsync 一次往返完成。
        /// </summary>
        public async Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size)
        {
            var repo = command.GetRepository<IUserRepository>();
            return await repo.QueryPagedAsync(name, page, size);
        }
    }
}
