using System.Threading;
using System.Threading.Tasks;
using Application.Users.Dtos;
using Frame.Core.Application;
using Frame.Core.Entities.Dtos;
using Frame.Core.Uow;

namespace Application.Users
{
    /// <summary>
    /// 用户应用服务。写方法标注 <see cref="UnitOfWorkAttribute"/>，
    /// 由框架生成的代理在方法成功返回后自动提交、抛异常时自动回滚。
    /// </summary>
    public interface IUserService : IApplicationService
    {
        /// <summary>分页查询用户</summary>
        Task<PageResult<UserDto>> GetPagedAsync(string? name, int page, int size, CancellationToken cancellationToken = default);

        /// <summary>按主键查询用户，不存在返回 null（Redis 缓存 30 秒）</summary>
        Task<UserDto?> GetAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>创建用户</summary>
        [UnitOfWork]
        Task<UserDto> CreateAsync(CreateUserInput input, CancellationToken cancellationToken = default);

        /// <summary>修改用户名称</summary>
        [UnitOfWork]
        Task RenameAsync(int id, string name, CancellationToken cancellationToken = default);

        /// <summary>删除用户（软删除）</summary>
        [UnitOfWork]
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
