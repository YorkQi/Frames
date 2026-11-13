---
title: 常见任务配方
audience: 使用者
status: stable
updated: 2026-11-13
sources:
  - samples/Webs/Application/Users/UserService.cs
  - samples/TaskBoard
  - src/Frame.Core/Uow/Interception/UnitOfWorkProxy.cs
---

# 常见任务配方

每条配方只列「要改哪几处 + 最小代码 + 验证方式」。机制解释在 `concepts/`，完整工程见 [示例工程](samples.md)。

## 1. 加一个聚合与表

1. `Domain/<聚合>/<Name>.cs`：继承 `Entity<int>`（或 `Entity<Guid>`），实现 `IFullAuditedObject` 以拿到审计与软删除；
2. 静态工厂 `Create(...)` 做参数校验，不要公开可变 setter；行为方法（`Rename`/`Archive`）在内部维护不变量；
3. 需要发事件时调用 `AddDomainEvent(new XxxDomainEvent(...))`（事件类型实现 `IEvent`，见配方 4）；
4. 建表 SQL 手写（框架不带迁移），字段与属性同名，`Id` 自增，补齐 `CreationTime`、`IsDeleted` 等审计列。

```csharp
[Table("Projects")]
public class Project : Entity<int>, IFullAuditedObject
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }

    private Project() { }                       // 供 EF 使用

    public static Project Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("编码不能为空。");
        var project = new Project { Code = code.Trim().ToUpperInvariant(), Name = name.Trim() };
        project.AddDomainEvent(new ProjectCreatedDomainEvent(project.Code, project.Name));
        return project;
    }
}
```

验证：`dotnet build`；仓储的 `InsertAsync` 返回 `0` 属于预期（延迟持久化）。

## 2. 加一个 CRUD 端点

以 Webs 的五层结构为准：

1. `Domain`：聚合 + `I<X>Repository`（继承 `IRepository<int, X>`，加自定义查询）；
2. `Repository`：实现仓储，SQL 手写，条件用 `DbParameters`，列表查询带 `IsDeleted = 0`；
3. `Application`：`I<X>Service : IApplicationService`（写方法标 `[UnitOfWork]`）+ DTO + 输入模型（DataAnnotations）；
4. `Web`：`[ApiController]` 控制器，只依赖 `I<X>Service`；
5. 在 `Program.cs` 的 `AddFrameService` 里 `UseApplication()` 与 `UseDatabaseContext<T>()`。

```csharp
[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(IProjectAppService service) : ControllerBase
{
    [HttpGet]
    public Task<PageResult<ProjectDto>> GetPagedAsync([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
        => service.GetPagedAsync(keyword, page, size, ct);

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateAsync(CreateProjectInput input, CancellationToken ct)
    {
        var dto = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetAsync), new { id = dto.Id }, dto);
    }
}
```

验证：`curl` 走一遍，再查库确认 `CreationTime`/`CreatorId` 已填。

## 3. 让写操作自动落库

三个条件缺一不可：

1. 服务通过 **接口** 解析（`I<X>Service`），并且接口或实现上有 `[UnitOfWork]`；
2. 方法内调用过 `databaseContext.GetRepository<T>()`（只有这一次调用会把 `IDbContext` 挂到当前 UOW）；
3. 本次调用处于工作单元内（由代理或 `BeginUnitOfWork` 开启）。

```csharp
public interface IProjectAppService : IApplicationService
{
    [UnitOfWork] Task<ProjectDto> CreateAsync(CreateProjectInput input, CancellationToken ct = default);
}
```

⚠️ 限制：写操作本身仍返回 `0`；若为了拿自增主键而显式 `SaveChangesAsync`，事件仍会被收集（框架从全部被追踪实体取事件）。没有 UOW 时必须自己 `SaveChangesAsync`，见 [工作单元](../concepts/unit-of-work.md#4-与数据库的集成)。

## 4. 发领域事件并处理

1. 事件：`sealed record ProjectCreatedDomainEvent(string Code, string Name) : IEvent;`
2. 聚合内 `AddDomainEvent(...)`（只在状态真正变化时发）；
3. 处理器：`sealed class ProjectCreatedHandler(ILogger<...> logger) : IEventHandler<ProjectCreatedDomainEvent>`；
4. 确认 `UseEventBus()` 已注册（它同时注册 `IDomainEventDispatcher`）。

⚠️ 限制：分发按运行期具体类型匹配处理器，继承/接口多态不会命中；提交后分发、无 outbox，崩溃会丢；处理器要幂等。

## 5. 软删除与查询过滤

1. 实体实现 `ISoftDelete`（`IFullAuditedObject` 已包含）；
2. 删除走 `repository.DeleteAsync(entity, ct)`，EF 通道在 `SaveChanges` 前把 `Deleted` 改写为 `IsDeleted = true` + 删除审计；
3. 所有手写查询自己加 `AND IsDeleted = 0`——框架没有全局查询过滤器。

```sql
SELECT * FROM `Projects` WHERE IsDeleted = 0 ORDER BY Id DESC
```

⚠️ 限制：Dapper 的 `Delete`/批量删除不经过审计改写；软删除记录仍占用唯一索引。

## 6. 加锁保护临界区

```csharp
public sealed class ProjectCodeGenerator(ILockFactory lockFactory)
{
    public async Task<string> NextAsync(CancellationToken ct)
    {
        await using var @lock = await lockFactory.CreateLockAsync("project:code");
        if (!@lock.IsAcquired) throw new BusinessException(3001, "获取锁失败，请重试。");
        return $"P-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}
```

要点：`UseLock(LockType.Local)` 仅单进程；多实例用 `LockType.Redis` 并配 `LockOptions`；拿不到锁 **不抛异常**，只返回 `IsAcquired = false`，必须自己判断。

## 7. 加定时任务

```csharp
[Scheduled("OverdueTaskScan", "0 */10 * * * *", "扫描逾期未完成的任务")]
public sealed class OverdueTaskScanJob(TaskBoardDatabaseContext db, ILogger<OverdueTaskScanJob> logger) : ISchedulerJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var repository = db.GetRepository<ITaskItemRepository>();
        foreach (var task in await repository.GetOverdueAsync(DateTime.UtcNow, cancellationToken))
            logger.LogWarning("任务逾期：{Title}", task.Title);
    }
}
```

宿主侧 `options.UseScheduler()`（扫描 `[Scheduled]`）或 `options.UseScheduler(jobSchedules)`（代码注册）；两种都要求 `AddFrameService` 与宿主启动。⚠️ 限制：Quartz 用内存 JobStore，重启不补跑。

## 8. 加 Redis 缓存

```csharp
public async Task<UserDto> GetAsync(int id, CancellationToken cancellationToken = default)
{
    var cache = redis.GetDbContext();               // IRedisDbContext
    var key = $"frames:users:{id}";

    var cached = cache.Get(key);
    if (!string.IsNullOrEmpty(cached)) return JsonSerializer.Deserialize<UserDto>(cached)!;

    var user = await command.GetRepository<IUserRepository>().GetAsync(id, cancellationToken)
               ?? throw new KeyNotFoundException($"用户 {id} 不存在。");
    var dto = ToDto(user);
    cache.Set(key, JsonSerializer.Serialize(dto), TimeSpan.FromSeconds(30));
    return dto;
}
```

要点：注册 `UseRedisDatabase<TRedisContext>(...ConnectionStringCollection)`；写入后主动删 key（示例 `UserService` 就是这么做的）；连接串是占位值时启动会同步连接失败，先改配置。

## 9. 多宿主共享装配（项目集）

一个领域、多个宿主时，不要在 `Web/Program.cs` 和 `Worker/Program.cs` 各写一遍注册：

1. `Infrastructure/TaskBoardServiceCollectionExtensions.cs` 暴露 `AddTaskBoard(this IServiceCollection, IConfiguration, Action<ServiceConfigurationContext>? configure = null)`；
2. 组合根内部做 `AddFrameService(o => { UseModule(); UseApplication(); UseDatabaseContext<T>(); UseEventBus(); configure?.Invoke(o); })`；
3. 各宿主只调 `AddTaskBoard(...)`，再追加自己的东西（Web 加控制器/过滤器，Worker 加 `UseScheduler()`）。

细节与完整分层见 [示例工程 §7](samples.md#7-项目集方案-taskboard)。

## 10. 给新代码补测试

1. 纯领域逻辑放 `samples/<X>/Tests`，只引用 `Domain`，不碰数据库；
2. 框架行为放 `Test/TestUnit`，需要内部类型时用 `InternalsVisibleTo`（`src/Frame.Core/AssemblyInfo.cs` 已开）；
3. 命名 `方法_场景_期望`；断言事件时检查 `DomainEvents` 并 `ClearDomainEvents()`；
4. 跑 `dotnet test Frames.sln`，数字变化同步 [manifest.json](../manifest.json) 的 `facts`。

⚠️ 没有集成测试：`IDbContext`、事件总线、Quartz 的真实运行路径目前只靠示例手工验证，补测试时优先 SQLite/Testcontainers。
