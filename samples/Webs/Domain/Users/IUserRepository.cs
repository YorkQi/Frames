using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Users.Enums;
using Frame.Core.Entities.Dtos;
using Frame.Core.Repositories;

namespace Domain.Users
{
    public interface IUserRepository :
        IRepository<int, User>
    {
        /// <summary>Dapper 原始 SQL 查询</summary>
        Task<IEnumerable<User>> QueryAsync();

        /// <summary>DbParameters 动态参数查询</summary>
        Task<IEnumerable<User>> QueryByConditionAsync(string? name, UserSex? sex, int? status);

        /// <summary>QueryPaged 分页查询</summary>
        Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size);
    }
}
