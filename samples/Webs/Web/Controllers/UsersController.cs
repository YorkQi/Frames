using Application.Users;
using Application.Users.Dtos;
using Frame.Core.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    /// <summary>用户接口：演示应用服务 + 工作单元 + 审计 + Redis 缓存。</summary>
    [ApiController]
    [Route("api/users")]
    public sealed class UsersController(IUserService service) : ControllerBase
    {
        /// <summary>分页查询用户</summary>
        [HttpGet]
        public async Task<ActionResult<PageResult<UserDto>>> GetPaged(
            [FromQuery] string? name,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await service.GetPagedAsync(name, page, size, cancellationToken);
            return Ok(result);
        }

        /// <summary>按主键查询用户</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await service.GetAsync(id, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        /// <summary>创建用户</summary>
        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserInput input, CancellationToken cancellationToken)
        {
            var user = await service.CreateAsync(input, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        /// <summary>修改用户名称</summary>
        [HttpPut("{id:int}/name")]
        public async Task<IActionResult> Rename(int id, [FromBody] RenameUserInput input, CancellationToken cancellationToken)
        {
            await service.RenameAsync(id, input.Name, cancellationToken);
            return NoContent();
        }

        /// <summary>删除用户（软删除）</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
