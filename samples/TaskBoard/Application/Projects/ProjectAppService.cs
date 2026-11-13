using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Exceptions;
using Application.Projects.Dtos;
using Domain.Projects;
using Domain.Tasks;
using Frame.Core.Application;
using Frame.Core.Auditing;
using Frame.Core.Entities.Dtos;
using Frame.Core.Uow;
using Infrastructure.DatabaseContexts;
using Microsoft.Extensions.Logging;

namespace Application.Projects
{
    /// <summary>
    /// 项目应用服务。写方法在 <see cref="IProjectAppService"/> 上标注 [UnitOfWork]，
    /// 因此方法返回前工作单元会统一落库 + 提交事务 + 分发领域事件。
    /// 注意：只有通过 <see cref="DatabaseContext.GetRepository{TRepository}"/> 取到的仓储
    /// 才会把当前 IDbContext 挂到工作单元上（这是自动落库的触发点）。
    /// </summary>
    public sealed class ProjectAppService(
        TaskBoardDatabaseContext databaseContext,
        IUnitOfWorkManager unitOfWorkManager,
        ICurrentUser currentUser,
        ILogger<ProjectAppService> logger)
        : ApplicationService(unitOfWorkManager), IProjectAppService
    {
        /// <inheritdoc />
        public async Task<PageResult<ProjectDto>> GetPagedAsync(string? keyword, int page, int size, CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<IProjectRepository>();
            var result = await repository.QueryPagedAsync(keyword, page, size, cancellationToken);

            var items = result.Items
                .Select(project => ToDto(project, Array.Empty<TaskItemDto>()))
                .ToArray();

            return new PageResult<ProjectDto>(items, result.Count);
        }

        /// <inheritdoc />
        public async Task<ProjectDto> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<IProjectRepository>();
            var project = await repository.GetAsync(id, cancellationToken);
            if (project is null)
                throw new KeyNotFoundException($"项目 {id} 不存在。");

            var taskRepository = databaseContext.GetRepository<ITaskItemRepository>();
            var tasks = await taskRepository.QueryByProjectAsync(id, cancellationToken);

            return ToDto(project, tasks.Select(ToDto).ToArray());
        }

        /// <inheritdoc />
        public async Task<ProjectDto> CreateAsync(CreateProjectInput input, CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<IProjectRepository>();

            if (await repository.ExistsCodeAsync(input.Code, cancellationToken))
                throw new BusinessException(1001, "项目编码已存在。");

            var ownerId = currentUser.Id ?? "anonymous";
            var project = Project.Create(input.Code, input.Name, input.Description, ownerId);

            await repository.InsertAsync(project, cancellationToken);

            // 创建后立刻需要数据库生成的自增主键；事务仍由工作单元提交。
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("项目 {Code} 已创建，Id={Id}", project.Code, project.Id);
            return ToDto(project, Array.Empty<TaskItemDto>());
        }

        /// <inheritdoc />
        public async Task<ProjectDto> RenameAsync(int id, RenameProjectInput input, CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<IProjectRepository>();
            var project = await GetRequiredAsync(repository, id, cancellationToken);

            project.Rename(input.Name);
            await repository.UpdateAsync(project, cancellationToken);

            return ToDto(project, Array.Empty<TaskItemDto>());
        }

        /// <inheritdoc />
        public async Task<ProjectDto> ArchiveAsync(int id, CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<IProjectRepository>();
            var project = await GetRequiredAsync(repository, id, cancellationToken);

            project.Archive();
            await repository.UpdateAsync(project, cancellationToken);

            return ToDto(project, Array.Empty<TaskItemDto>());
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<IProjectRepository>();
            var project = await GetRequiredAsync(repository, id, cancellationToken);

            // 实体实现了 ISoftDelete，框架在保存时把物理删除改写成 IsDeleted = true。
            await repository.DeleteAsync(project.Id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TaskItemDto> AddTaskAsync(int projectId, AddTaskInput input, CancellationToken cancellationToken = default)
        {
            var projectRepository = databaseContext.GetRepository<IProjectRepository>();
            await GetRequiredAsync(projectRepository, projectId, cancellationToken);

            var repository = databaseContext.GetRepository<ITaskItemRepository>();
            var task = TaskItem.Create(projectId, input.Title, input.DueDate);
            await repository.InsertAsync(task, cancellationToken);

            return ToDto(task);
        }

        /// <inheritdoc />
        public async Task<TaskItemDto> CompleteTaskAsync(int taskId, CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<ITaskItemRepository>();
            var task = await repository.GetAsync(taskId, cancellationToken);
            if (task is null)
                throw new KeyNotFoundException($"任务 {taskId} 不存在。");

            task.Complete();
            await repository.UpdateAsync(task, cancellationToken);

            return ToDto(task);
        }

        private static async Task<Project> GetRequiredAsync(IProjectRepository repository, int id, CancellationToken cancellationToken)
        {
            var project = await repository.GetAsync(id, cancellationToken);
            if (project is null)
                throw new KeyNotFoundException($"项目 {id} 不存在。");

            return project;
        }

        private static ProjectDto ToDto(Project project, IReadOnlyList<TaskItemDto> tasks) => new(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            project.OwnerId,
            project.Status.ToString(),
            project.CreationTime,
            project.LastModificationTime,
            tasks);

        private static TaskItemDto ToDto(TaskItem task) => new(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Status.ToString(),
            task.DueDate);
    }
}
