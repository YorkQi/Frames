using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Users.Enums;
using Frame.Core.Entities.Dtos;
using Frame.Core.Repositories;

namespace Domain.Users
{
    /// <summary>用户仓储：框架自动把实现类注册为 Scoped。</summary>
    public interface IUserRepository : IRepository<int, User>
    {
        /// <summary>查询未删除的前 N 个用户（Dapper 原始 SQL 示例）</summary>
        Task<IEnumerable<User>> QueryAsync(CancellationToken cancellationToken = default);

        /// <summary>DbParameters 动态条件查询</summary>
        Task<IEnumerable<User>> QueryByConditionAsync(string? name, UserSex? sex, CancellationToken cancellationToken = default);

        /// <summary>分页查询（一次往返完成 COUNT + 数据）</summary>
        Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size, CancellationToken cancellationToken = default);

        /// <summary>名称是否已被占用（未删除范围内）</summary>
        Task<bool> ExistsNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 立即保存尚未落库的变更（EF Core 通道的写操作默认只挂起）。
        /// 用于需要数据库自增主键的场景；事务仍由工作单元统一提交。
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
