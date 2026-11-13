using Application.Abstractions;
using Application.Projects.Dtos;
using Frame.Core.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    /// <summary>项目与任务的 REST 接口。</summary>
    [ApiController]
    [Route("api")]
    public sealed class ProjectsController(IProjectAppService service) : ControllerBase
    {
        /// <summary>分页查询项目</summary>
        [HttpGet("projects")]
        public async Task<ActionResult<PageResult<ProjectDto>>> GetPaged(
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            CancellationToken cancellationToken = default)
            => Ok(await service.GetPagedAsync(keyword, page, size, cancellationToken));

        /// <summary>查询项目详情（含任务列表）</summary>
        [HttpGet("projects/{id:int}")]
        public async Task<ActionResult<ProjectDto>> GetById(int id, CancellationToken cancellationToken)
            => Ok(await service.GetAsync(id, cancellationToken));

        /// <summary>创建项目</summary>
        [HttpPost("projects")]
        public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectInput input, CancellationToken cancellationToken)
        {
            var project = await service.CreateAsync(input, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }

        /// <summary>项目改名</summary>
        [HttpPut("projects/{id:int}/name")]
        public async Task<ActionResult<ProjectDto>> Rename(int id, [FromBody] RenameProjectInput input, CancellationToken cancellationToken)
            => Ok(await service.RenameAsync(id, input, cancellationToken));

        /// <summary>归档项目</summary>
        [HttpPost("projects/{id:int}/archive")]
        public async Task<ActionResult<ProjectDto>> Archive(int id, CancellationToken cancellationToken)
            => Ok(await service.ArchiveAsync(id, cancellationToken));

        /// <summary>删除项目（软删除）</summary>
        [HttpDelete("projects/{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        /// <summary>在项目下新增任务</summary>
        [HttpPost("projects/{id:int}/tasks")]
        public async Task<ActionResult<TaskItemDto>> AddTask(int id, [FromBody] AddTaskInput input, CancellationToken cancellationToken)
            => Ok(await service.AddTaskAsync(id, input, cancellationToken));

        /// <summary>完成任务</summary>
        [HttpPost("tasks/{taskId:int}/complete")]
        public async Task<ActionResult<TaskItemDto>> CompleteTask(int taskId, CancellationToken cancellationToken)
            => Ok(await service.CompleteTaskAsync(taskId, cancellationToken));
    }
}
