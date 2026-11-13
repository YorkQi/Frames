using System.Threading;
using System.Threading.Tasks;
using Application.Projects.Dtos;
using Frame.Core.Application;
using Frame.Core.Entities.Dtos;
using Frame.Core.Uow;

namespace Application.Abstractions
{
    /// <summary>
    /// 项目应用服务。写方法在接口上标注 <see cref="UnitOfWorkAttribute"/>，
    /// 由 UseApplication 生成代理：方法返回前提交工作单元（含落库与领域事件分发），
    /// 抛异常则回滚。
    /// </summary>
    public interface IProjectAppService : IApplicationService
    {
        /// <summary>分页查询项目</summary>
        Task<PageResult<ProjectDto>> GetPagedAsync(string? keyword, int page, int size, CancellationToken cancellationToken = default);

        /// <summary>查询项目详情（含任务列表）</summary>
        Task<ProjectDto> GetAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>创建项目</summary>
        [UnitOfWork]
        Task<ProjectDto> CreateAsync(CreateProjectInput input, CancellationToken cancellationToken = default);

        /// <summary>项目改名</summary>
        [UnitOfWork]
        Task<ProjectDto> RenameAsync(int id, RenameProjectInput input, CancellationToken cancellationToken = default);

        /// <summary>归档项目</summary>
        [UnitOfWork]
        Task<ProjectDto> ArchiveAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>删除项目（软删除）</summary>
        [UnitOfWork]
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>在项目下新增任务</summary>
        [UnitOfWork]
        Task<TaskItemDto> AddTaskAsync(int projectId, AddTaskInput input, CancellationToken cancellationToken = default);

        /// <summary>完成任务</summary>
        [UnitOfWork]
        Task<TaskItemDto> CompleteTaskAsync(int taskId, CancellationToken cancellationToken = default);
    }
}
