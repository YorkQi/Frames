using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Users.Dtos;
using Domain.Users;
using Frame.Core.Application;
using Frame.Core.Auditing;
using Frame.Core.Entities.Dtos;
using Frame.Core.Uow;
using Infrastructure.DatabaseContexts;
using Infrastructure.RedisContexts;
using Microsoft.Extensions.Logging;

namespace Application.Users
{
    /// <summary>
    /// 用户应用服务示例：
    /// 写操作由 <see cref="UnitOfWorkAttribute"/> 自动提交，读操作带一层 Redis 缓存。
    /// </summary>
    public sealed class UserService(
        CommandDatabaseContext command,
        CommandRedisContext redis,
        IUnitOfWorkManager unitOfWorkManager,
        ICurrentUser currentUser,
        ILogger<UserService> logger) : ApplicationService(unitOfWorkManager), IUserService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public async Task<PageResult<UserDto>> GetPagedAsync(string? name, int page, int size, CancellationToken cancellationToken = default)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "页码必须 >= 1。");
            if (size is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(size), "每页条数必须在 1-200 之间。");

            var repository = command.GetRepository<IUserRepository>();
            var result = await repository.QueryPagedAsync(name, page, size, cancellationToken);

            return new PageResult<UserDto>(result.Items.Select(ToDto).ToList(), result.Count);
        }

        /// <inheritdoc />
        public async Task<UserDto?> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            var cache = redis.GetDbContext();
            var cacheKey = CacheKey(id);

            var cached = cache.Get(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                var hit = JsonSerializer.Deserialize<UserDto>(cached);
                if (hit is not null)
                {
                    logger.LogDebug("命中用户缓存：{UserId}", id);
                    return hit;
                }
            }

            var repository = command.GetRepository<IUserRepository>();
            var user = await repository.GetAsync(id, cancellationToken);
            if (user is null || user.IsDeleted)
                return null;

            var dto = ToDto(user);
            cache.Set(cacheKey, JsonSerializer.Serialize(dto), CacheTtl);
            return dto;
        }

        /// <inheritdoc />
        [UnitOfWork]
        public async Task<UserDto> CreateAsync(CreateUserInput input, CancellationToken cancellationToken = default)
        {
            var repository = command.GetRepository<IUserRepository>();

            if (await repository.ExistsNameAsync(input.Name, cancellationToken))
                throw new BusinessException($"用户名称“{input.Name.Trim()}”已存在。");

            var user = User.Create(input.Name, input.Sex, input.ProfilePicture);
            await repository.InsertAsync(user, cancellationToken);

            // EF Core 通道的写操作默认只挂起，需要自增主键时显式保存；
            // 事务仍由工作单元在方法返回后统一提交。
            await repository.SaveChangesAsync(cancellationToken);

            redis.GetDbContext().DeleteKey(CacheKey(user.Id));
            logger.LogInformation("用户 {UserId} 已创建，操作人 {OperatorId}", user.Id, currentUser.Id ?? "anonymous");

            return ToDto(user);
        }

        /// <inheritdoc />
        [UnitOfWork]
        public async Task RenameAsync(int id, string name, CancellationToken cancellationToken = default)
        {
            var repository = command.GetRepository<IUserRepository>();
            var user = await repository.GetAsync(id, cancellationToken);

            if (user is null || user.IsDeleted)
                throw new KeyNotFoundException($"用户 {id} 不存在。");

            user.Rename(name);
            await repository.UpdateAsync(user, cancellationToken);

            redis.GetDbContext().DeleteKey(CacheKey(id));
            logger.LogInformation("用户 {UserId} 已改名，操作人 {OperatorId}", id, currentUser.Id ?? "anonymous");
        }

        /// <inheritdoc />
        [UnitOfWork]
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var repository = command.GetRepository<IUserRepository>();
            var user = await repository.GetAsync(id, cancellationToken);

            if (user is null || user.IsDeleted)
                throw new KeyNotFoundException($"用户 {id} 不存在。");

            // 实体实现 ISoftDelete，框架在保存时把物理删除改写为软删除。
            await repository.DeleteAsync(id, cancellationToken);

            redis.GetDbContext().DeleteKey(CacheKey(id));
            logger.LogInformation("用户 {UserId} 已删除，操作人 {OperatorId}", id, currentUser.Id ?? "anonymous");
        }

        private static string CacheKey(int id) => $"frames:users:{id}";

        private static UserDto ToDto(User user) => new(
            user.Id,
            user.Name,
            user.Sex,
            user.ProfilePicture,
            user.CreationTime,
            user.LastModificationTime,
            user.IsDeleted);
    }
}
