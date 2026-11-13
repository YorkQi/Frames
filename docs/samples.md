# 示例工程

`samples/` 下有四个可运行工程，演示框架在不同形态下的用法：

| 示例 | 类型 | 演示内容 | 外部依赖 | 启动 |
| --- | --- | --- | --- | --- |
| `samples/Webs/Web` | ASP.NET Core Web API | 应用服务 + `[UnitOfWork]` + 审计 + 软删除 + 领域事件 + Redis 缓存 | MySQL、Redis | `dotnet run --project samples/Webs/Web` |
| `samples/Scheduler/Job` | Web SDK 控制台 | Quartz + `JobSchedule` 列表注册 | 无 | `dotnet run --project samples/Scheduler/Job` |
| `samples/TaskBoard/Web` | Web API | **小型项目集**：多个宿主共用一套组合根 | MySQL | `dotnet run --project samples/TaskBoard/Web` |
| `samples/TaskBoard/Worker` | Worker Service | `[Scheduled]` 属性发现 + 定时扫描 | MySQL | `dotnet run --project samples/TaskBoard/Worker` |

三个示例都只引用 `src/`，不被任何项目引用。`samples/Webs` 强调「分层最完整」，`samples/TaskBoard` 强调「一个项目集怎么组织」。

## 1. Webs 的结构与引用链

```
samples/Webs/
  Domain/                       实体、值对象、领域事件、仓储接口
    Users/User.cs
    Users/UserSex.cs
    Users/IUserRepository.cs
    Users/Events/UserRegisteredDomainEvent.cs
    Users/Events/UserRenamedDomainEvent.cs
  Application/                  应用服务、DTO、业务异常
    Users/IUserService.cs
    Users/UserService.cs
    Users/Dtos/{UserDto,CreateUserInput,RenameUserInput}.cs
    Exceptions/BusinessException.cs
  Repository/                   仓储实现（Dapper + EF 双通道）
    UserRepository.cs
  Infrastructure/               数据库上下文与 Redis 上下文
    DatabaseContexts/{CommandDatabaseContext,QueryDatabaseContext}.cs
    RedisContexts/{CommandRedisContext,QueryRedisContext}.cs
  Web/                          HTTP 入口
    Controllers/UsersController.cs
    Filters/ExceptionFilter.cs
    Exceptions/ExceptionResult.cs
    EventBus/UserRegisteredDomainEventHandler.cs
    Security/HttpContextCurrentUser.cs
    Program.cs
    appsettings.json
```

| 项目 | 引用 |
| --- | --- |
| `Domain` | `Frame.Core`、`Frame.EventBus`（领域事件实现 `IEvent`） |
| `Repository` | `Domain`、`Frame.Databases` |
| `Infrastructure` | `Repository`、`Frame.EventBus`、`Frame.Locks`、`Frame.Redis` |
| `Application` | `Infrastructure` |
| `Web` | `Application` |

`Application → Infrastructure` 是**当前框架约束下的必要耦合**：自动落库的触发点是 `DatabaseContext.GetRepository<T>()`，应用服务必须拿到具体的 `DatabaseContext` 才能让框架把 `IDbContext` 挂到工作单元上。理想形态是只依赖仓储接口，见 [设计说明第 11 节](design-notes.md#11-如果只能做三件事)。

## 2. 运行 Web 示例

### 2.1 先补配置

`samples/Webs/Web/appsettings.json` 里的连接串是占位值，`Password=your-password` 必须改成真实密码：

```json
{
  "DBConnections": {
    "Command": "Server=localhost;Port=3306;Database=frames;User=root;Password=your-password;CharSet=utf8mb4;",
    "Query": "Server=localhost;Port=3306;Database=frames;User=root;Password=your-password;CharSet=utf8mb4;"
  },
  "RedisDBConnections": {
    "Command": "localhost:6379",
    "Query": "localhost:6379"
  }
}
```

`Program.cs` 用 `ReadConnections` 读取这四个值，缺任何一节都会在启动时抛：

```
InvalidOperationException: 缺少配置节 DBConnections:Command
```

Command / Query 两套连接串是为了演示「读写分离」的组织方式：`CommandDatabaseContext` 用 Command，`QueryDatabaseContext` 用 Query。示例里两者指向同一个库。

### 2.2 建表

框架不带迁移，先手工执行 [第 5 节](#5-数据库与表结构) 的 MySQL 脚本。

### 2.3 启动

```bash
dotnet run --project samples/Webs/Web
```

看到 `Now listening on: http://localhost:5xxx` 即可。健康检查：

```bash
curl http://localhost:5000/health
```

### 2.4 用 curl 走一遍

```bash
# 创建
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"alice","sex":1,"profilePicture":"https://example.com/a.png"}'

# 分页查询
curl "http://localhost:5000/api/users?name=ali&page=1&size=20"

# 详情（第一次读库并写缓存，30 秒内再次请求命中 Redis）
curl http://localhost:5000/api/users/1

# 改名
curl -X PUT http://localhost:5000/api/users/1/name \
  -H "Content-Type: application/json" -d '{"name":"alice-2"}'

# 删除（软删除：行仍在，IsDeleted = 1）
curl -X DELETE http://localhost:5000/api/users/1

# 再查详情：404
curl -i http://localhost:5000/api/users/1
```

想确认软删除，直接查库：

```sql
SELECT Id, Name, IsDeleted, DeletionTime, DeleterId FROM Users;
```

## 3. 各层代码

### 3.1 Domain

```csharp
public class User : Entity<int>, IFullAuditedObject
{
    public string Name { get; private set; } = string.Empty;
    public UserSex Sex { get; private set; }
    public string? ProfilePicture { get; private set; }

    // 审计字段由框架填充，实体只声明
    public DateTime CreationTime { get; set; }
    public string? CreatorId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public string? LastModifierId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
    public string? DeleterId { get; set; }

    public static User Create(string name, UserSex sex, string? profilePicture = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("用户名不能为空。", nameof(name));

        var user = new User { Name = name.Trim(), Sex = sex, ProfilePicture = profilePicture };
        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Name));
        return user;
    }

    public void Rename(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("用户名不能为空。", nameof(name));
        if (trimmed == Name)
            return;

        Name = trimmed;
        AddDomainEvent(new UserRenamedDomainEvent(Id, trimmed));
    }
}
```

要点：

- 实体自己保证不变量，应用服务只做编排；
- 领域事件在**构造/状态变化时**加入，由工作单元在提交后分发；
- 审计属性只是普通属性，填充发生在 `SaveChanges` 之前（见 [审计字段](auditing.md)）。

### 3.2 Repository

```csharp
public sealed class UserRepository : Repository<int, User>, IUserRepository
{
    /// <inheritdoc />
    public async Task<bool> ExistsNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var count = await DBContext.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM `Users` WHERE Name = @Name AND IsDeleted = 0",
            new DbParameters().Add("Name", name),
            cancellationToken: cancellationToken);
        return count > 0;
    }

    /// <inheritdoc />
    public async Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size, CancellationToken cancellationToken = default)
    {
        var sql = "SELECT * FROM `Users` WHERE IsDeleted = 0";
        var parameters = new DbParameters();

        if (!string.IsNullOrWhiteSpace(name))
        {
            sql += " AND (Name LIKE @Name OR ProfilePicture LIKE @Name)";
            parameters.Add("Name", $"%{name.Trim()}%");
        }

        sql += " ORDER BY Id DESC";
        return await DBContext.QueryPagedAsync<User>(sql, parameters, page, size, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => DBContext.SaveChangesAsync(cancellationToken);
}
```

两个细节：

- **软删除过滤要手写**：框架没有全局查询过滤器，SQL 里统一带 `IsDeleted = 0`；
- `SaveChangesAsync` 暴露给应用层，用于「创建后立刻需要自增主键」的场景；正常路径由工作单元在方法返回前统一提交。

### 3.3 Application

```csharp
public interface IUserService : IApplicationService
{
    Task<PageResult<UserDto>> GetPagedAsync(string? name, int page, int size, CancellationToken cancellationToken = default);
    Task<UserDto> GetAsync(int id, CancellationToken cancellationToken = default);

    [UnitOfWork]
    Task<UserDto> CreateAsync(CreateUserInput input, CancellationToken cancellationToken = default);

    [UnitOfWork]
    Task RenameAsync(int id, string name, CancellationToken cancellationToken = default);

    [UnitOfWork]
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

`UseApplication()` 检测到接口或接口方法上有 `[UnitOfWork]`，就用 `DispatchProxy` 生成代理：方法返回前 `CompleteAsync`（落库 + 提交 + 分发事件），抛异常则 `RollbackAsync`。详见 [工作单元与自动事务](unit-of-work.md)。

实现里三件事值得注意：

```csharp
public async Task<UserDto> CreateAsync(CreateUserInput input, CancellationToken cancellationToken = default)
{
    var repository = command.GetRepository<IUserRepository>();

    if (await repository.ExistsNameAsync(input.Name, cancellationToken))
        throw new BusinessException(1001, "用户名已存在。");

    var user = User.Create(input.Name, input.Sex, input.ProfilePicture);
    await repository.InsertAsync(user, cancellationToken);

    // 需要数据库生成的自增主键，先落一次；事务仍由工作单元提交。
    await repository.SaveChangesAsync(cancellationToken);
    await redis.GetDbContext().KeyDeleteAsync(CacheKey(user.Id));

    return ToDto(user);
}
```

1. 业务冲突抛 `BusinessException`（带 `Code`/`StateCode`），由 `ExceptionFilter` 转成 JSON；
2. 创建后要返回自增 Id，所以显式 `SaveChangesAsync`；其余写方法让工作单元提交；
3. 写路径主动删缓存，读路径 `GetAsync` 用 `StringGetAsync` → 未命中查库 → `StringSetAsync(... TimeSpan.FromSeconds(30))`。

### 3.4 Web

`Program.cs` 的装配顺序：

```csharp
builder.Services.AddControllers(options => options.Filters.Add<ExceptionFilter>());
builder.Services.AddHttpContextAccessor();

var command = ReadConnections(builder.Configuration, "DBConnections", "Command");
var query = ReadConnections(builder.Configuration, "DBConnections", "Query");
var redisCommand = ReadConnections(builder.Configuration, "RedisDBConnections", "Command");
var redisQuery = ReadConnections(builder.Configuration, "RedisDBConnections", "Query");

builder.Services.AddFrameService(options =>
{
    options.UseModule();
    options.UseApplication();
    options.UseDatabaseContext<CommandDatabaseContext>(new ConnectionStringCollection(command));
    options.UseDatabaseContext<QueryDatabaseContext>(new ConnectionStringCollection(query));
    options.UseRedisDatabase<CommandRedisContext>(new RedisConnectionStringCollection(redisCommand));
    options.UseRedisDatabase<QueryRedisContext>(new RedisConnectionStringCollection(redisQuery));
    options.UseLock(LockType.Local);
    options.UseEventBus();
});

// 覆盖框架默认的 NullCurrentUser：必须在 AddFrameService 之后注册。
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
```

`HttpContextCurrentUser` 从 `ClaimTypes.NameIdentifier`（或 `sub`）读用户 Id，从 `Identity.Name`（或 `name`）读用户名，供审计字段填充。

`ExceptionFilter` 只处理两类异常：

| 异常 | 响应 |
| --- | --- |
| `BusinessException` | `{ "code": 1001, "message": "用户名已存在。" }`，HTTP `StateCode ?? 400` |
| `KeyNotFoundException` | `{ "message": "用户 1 不存在。" }`，HTTP 404 |

## 4. 一次 POST /api/users 的完整链路

1. MVC 绑定 `CreateUserInput`，DataAnnotations 校验；
2. 进入 `UserService` 代理（接口方法带 `[UnitOfWork]`）→ `UnitOfWorkManager.Begin`；
3. `ExistsNameAsync` → Dapper 查重；
4. `User.Create` → 加入 `UserRegisteredDomainEvent`；
5. `InsertAsync` → EF `DbSet.Add`，**返回 0（挂起）**；
6. 显式 `SaveChangesAsync` → 审计填充、生成 Id、写库（事务内的第一条语句）；
7. 删 Redis 缓存，返回 DTO；
8. 代理 `CompleteAsync` → 从**所有被追踪实体**上取挂起的领域事件（此时用户实体已是 `Unchanged`，但事件仍挂着）→ 有变更才 `SaveChangesAsync` + `CommitAsync`（这里已无变更，直接跳过）→ 分发 `UserRegisteredDomainEvent`；
9. `IDomainEventDispatcher` 把 `UserRegisteredDomainEvent` 投递到 `LocalEventBus` 队列；
10. 后台 `EventBusHostService` 解析 `UserRegisteredDomainEventHandler` 并执行；
11. `201 Created` + `Location: /api/users/1`。

## 5. 数据库与表结构

Webs 需要 `frames` 库：

```sql
CREATE DATABASE IF NOT EXISTS `frames` DEFAULT CHARSET utf8mb4;
USE `frames`;

CREATE TABLE `Users`
(
    `Id`                 INT          NOT NULL AUTO_INCREMENT,
    `Name`               VARCHAR(64)  NOT NULL,
    `Sex`                INT          NOT NULL,
    `ProfilePicture`     VARCHAR(512) NULL,
    `CreationTime`       DATETIME(6)  NOT NULL,
    `CreatorId`          VARCHAR(64)  NULL,
    `LastModificationTime` DATETIME(6) NULL,
    `LastModifierId`     VARCHAR(64)  NULL,
    `IsDeleted`          TINYINT(1)   NOT NULL DEFAULT 0,
    `DeletionTime`       DATETIME(6)  NULL,
    `DeleterId`          VARCHAR(64)  NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Users_Name` (`Name`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
```

TaskBoard 需要 `taskboard` 库：

```sql
CREATE DATABASE IF NOT EXISTS `taskboard` DEFAULT CHARSET utf8mb4;
USE `taskboard`;

CREATE TABLE `Projects`
(
    `Id`                   INT          NOT NULL AUTO_INCREMENT,
    `Code`                 VARCHAR(32)  NOT NULL,
    `Name`                 VARCHAR(128) NOT NULL,
    `Description`          VARCHAR(512) NULL,
    `OwnerId`              VARCHAR(64)  NOT NULL,
    `Status`               INT          NOT NULL,
    `CreationTime`         DATETIME(6)  NOT NULL,
    `CreatorId`            VARCHAR(64)  NULL,
    `LastModificationTime` DATETIME(6)  NULL,
    `LastModifierId`       VARCHAR(64)  NULL,
    `IsDeleted`            TINYINT(1)   NOT NULL DEFAULT 0,
    `DeletionTime`         DATETIME(6)  NULL,
    `DeleterId`            VARCHAR(64)  NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Projects_Code` (`Code`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

CREATE TABLE `TaskItems`
(
    `Id`                   INT          NOT NULL AUTO_INCREMENT,
    `ProjectId`            INT          NOT NULL,
    `Title`                VARCHAR(200) NOT NULL,
    `Status`               INT          NOT NULL,
    `DueDate`              DATETIME(6)  NULL,
    `CreationTime`         DATETIME(6)  NOT NULL,
    `CreatorId`            VARCHAR(64)  NULL,
    `LastModificationTime` DATETIME(6)  NULL,
    `LastModifierId`       VARCHAR(64)  NULL,
    `IsDeleted`            TINYINT(1)   NOT NULL DEFAULT 0,
    `DeletionTime`         DATETIME(6)  NULL,
    `DeleterId`            VARCHAR(64)  NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_TaskItems_ProjectId` (`ProjectId`),
    KEY `IX_TaskItems_DueDate` (`DueDate`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
```

注意：

- 唯一性由应用层保证（`ExistsCodeAsync` / `ExistsNameAsync`），示例没有建唯一索引，避免和软删除后的行冲突；
- EF Core 默认把 `string` 映射成 `longtext`，这里用 `VARCHAR` 是为了兼容索引；列类型只要能和模型对上即可；
- 时间统一按 UTC 存储，展示层自行转换。

## 6. 调度示例（samples/Scheduler/Job）

```csharp
builder.Services.AddFrameService(options =>
{
    options.UseModule();
    options.UseApplication();

    // 也可以不传列表，让 UseScheduler() 扫描 [Scheduled]；
    // 这里演示「代码里登记 JobSchedule」的写法。
    var jobs = new List<JobSchedule>
    {
        new("TestScheduler", typeof(Job.Tasks.TestScheduler).FullName!, "0/5 * * * * *"),
        new("Test2Scheduler", typeof(Job.Tasks.Test2Scheduler).FullName!, "0/5 * * * * *"),
    };
    options.UseScheduler(jobs);
});
```

- `TestScheduler` / `Test2Scheduler` 只有 `Console.WriteLine`，用于确认触发器按 5 秒一次执行；
- `Test3Scheduler` 标了 `[Scheduled("Test3Scheduler", "0/5 * * * * *")]`，只有走属性发现（`UseScheduler()` 无参）才会注册——示例里那行被注释了，这是有意的对照；
- 实际项目更推荐属性发现，见 [TaskBoard Worker](#76-worker-定时任务)；
- Quartz 使用内存 JobStore，进程重启后作业重新注册，错过的触发不会补跑。

## 7. 项目集方案 TaskBoard

### 7.1 为什么单独做一个项目集

Webs 是「一个 Web 应用怎么用框架」，TaskBoard 回答另一个问题：**一个产品由 Web + 后台任务 + 测试组成时，代码怎么放、组合根怎么共享**。

它的目标不是功能完整，而是给出可以直接复制的骨架：领域模型有真实不变量、应用服务有真实事务边界、Worker 与 Web 共用一套装配。

### 7.2 分层与引用

```
samples/TaskBoard/
  Domain/                        聚合、不变量、领域事件、仓储接口
    Projects/{Project,ProjectStatus,IProjectRepository}.cs
    Projects/Events/{ProjectCreated,ProjectRenamed,ProjectArchived}DomainEvent.cs
    Tasks/{TaskItem,TaskItemStatus,ITaskItemRepository}.cs
    Tasks/Events/TaskCompletedDomainEvent.cs
    DomainException.cs
  Application/                   用例编排、DTO、业务异常
    Abstractions/IProjectAppService.cs
    Projects/ProjectAppService.cs
    Projects/Dtos/{ProjectDto,TaskItemDto,CreateProjectInput,RenameProjectInput,AddTaskInput}.cs
    Exceptions/BusinessException.cs
  Infrastructure/                持久化 + 组合根
    DatabaseContexts/TaskBoardDatabaseContext.cs
    Repositories/{ProjectRepository,TaskItemRepository}.cs
    TaskBoardServiceCollectionExtensions.cs      ← AddTaskBoard
  Web/                           HTTP 宿主
    Program.cs
    Controllers/ProjectsController.cs
    Filters/ApiExceptionFilter.cs
    Security/HttpContextCurrentUser.cs
    appsettings.json
  Worker/                        后台宿主
    Program.cs
    Jobs/OverdueTaskScanJob.cs
  Tests/                         聚合单元测试
    ProjectTests.cs
    TaskItemTests.cs
```

| 项目 | 引用 |
| --- | --- |
| `Domain` | `Frame.Core`、`Frame.EventBus` |
| `Infrastructure` | `Domain`、`Frame.Databases`、`Frame.EventBus`、`Frame.Locks` |
| `Application` | `Infrastructure` |
| `Web` | `Application` |
| `Worker` | `Infrastructure`、`Frame.Scheduler` |
| `Tests` | `Domain`（不碰数据库） |

### 7.3 组合根：AddTaskBoard

两个宿主都不重复写装配，只调用一个扩展方法：

```csharp
public static IServiceCollection AddTaskBoard(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<ServiceConfigurationContext>? configure = null)
{
    var connection = configuration["TaskBoard:Connections:Database"]
        ?? throw new InvalidOperationException("缺少配置项 TaskBoard:Connections:Database");

    services.AddFrameService(options =>
    {
        options.UseModule();
        options.UseApplication();
        options.UseDatabaseContext<TaskBoardDatabaseContext>(new ConnectionStringCollection(connection));
        options.UseEventBus();
        options.UseLock(LockType.Local);

        // 宿主追加自己的能力（Worker 用这一行打开调度）
        configure?.Invoke(options);
    });

    return services;
}
```

Web 宿主：

```csharp
builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddTaskBoard(builder.Configuration);
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
```

Worker 宿主：

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTaskBoard(builder.Configuration, options => options.UseScheduler());
await builder.Build().RunAsync();
```

`UseScheduler()` 无参时扫描 `[Scheduled]`，所以 Worker 只需标注作业类。

### 7.4 模型与不变量

`Project`：`Code`（创建时转大写）、`Name`、`Description`、`OwnerId`、`Status(Active/Archived)`。

- `Create` 校验编码/名称非空，发 `ProjectCreatedDomainEvent`；
- `Rename` 归档后抛 `DomainException("项目已归档，不能改名。")`，同名不产生事件；
- `Archive` 幂等性由调用方保证：重复归档抛异常，成功发 `ProjectArchivedDomainEvent`。

`TaskItem`：`ProjectId`、`Title`、`Status(Todo/Doing/Done)`、`DueDate`。

- `Create` 校验 `projectId > 0`、标题非空，`DueDate` 统一转 UTC；
- `Complete` 置 `Done` 并发 `TaskCompletedDomainEvent`，重复完成抛异常；
- `Start` 在 `Done` 状态抛异常。

两个聚合都实现 `IFullAuditedObject`，删除走软删除。

### 7.5 应用服务与端点

写方法全部在**接口**上标 `[UnitOfWork]`：

```csharp
public interface IProjectAppService : IApplicationService
{
    Task<PageResult<ProjectDto>> GetPagedAsync(string? keyword, int page, int size, CancellationToken cancellationToken = default);
    Task<ProjectDto> GetAsync(int id, CancellationToken cancellationToken = default);

    [UnitOfWork] Task<ProjectDto> CreateAsync(CreateProjectInput input, CancellationToken cancellationToken = default);
    [UnitOfWork] Task<ProjectDto> RenameAsync(int id, RenameProjectInput input, CancellationToken cancellationToken = default);
    [UnitOfWork] Task<ProjectDto> ArchiveAsync(int id, CancellationToken cancellationToken = default);
    [UnitOfWork] Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    [UnitOfWork] Task<TaskItemDto> AddTaskAsync(int projectId, AddTaskInput input, CancellationToken cancellationToken = default);
    [UnitOfWork] Task<TaskItemDto> CompleteTaskAsync(int taskId, CancellationToken cancellationToken = default);
}
```

| 方法 | 路由 | 说明 |
| --- | --- | --- |
| GET | `/api/projects?keyword=&page=1&size=20` | 分页，`keyword` 匹配 Code/Name |
| GET | `/api/projects/{id}` | 详情，含任务列表 |
| POST | `/api/projects` | 创建，`201 + Location` |
| PUT | `/api/projects/{id}/name` | 改名 |
| POST | `/api/projects/{id}/archive` | 归档 |
| DELETE | `/api/projects/{id}` | 软删除，`204` |
| POST | `/api/projects/{id}/tasks` | 新增任务 |
| POST | `/api/tasks/{taskId}/complete` | 完成任务 |

```bash
curl -X POST http://localhost:5000/api/projects \
  -H "Content-Type: application/json" \
  -d '{"code":"apollo","name":"Apollo","description":"示例项目"}'

curl -X POST http://localhost:5000/api/projects/1/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"写文档","dueDate":"2026-12-01T00:00:00Z"}'

curl -X POST http://localhost:5000/api/tasks/1/complete
```

错误映射（`ApiExceptionFilter`）：

| 异常 | HTTP | Body |
| --- | --- | --- |
| `BusinessException` | `StateCode ?? 400` | `{ "code": 1001, "message": "项目编码已存在。" }` |
| `DomainException` | 400 | `{ "code": 2001, "message": "项目已归档，不能改名。" }` |
| `KeyNotFoundException` | 404 | `{ "code": 2002, "message": "项目 1 不存在。" }` |

### 7.6 Worker 定时任务

```csharp
[Scheduled("OverdueTaskScan", "0 */10 * * * *", "扫描逾期未完成的任务")]
public sealed class OverdueTaskScanJob(
    TaskBoardDatabaseContext databaseContext,
    ILogger<OverdueTaskScanJob> logger) : ISchedulerJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var repository = databaseContext.GetRepository<ITaskItemRepository>();
        var overdue = await repository.GetOverdueAsync(DateTime.UtcNow, cancellationToken);

        foreach (var task in overdue)
            logger.LogWarning("任务 {TaskId} 已逾期：{Title}", task.Id, task.Title);
    }
}
```

`UseScheduler()` 扫描 `[Scheduled]` 注册 Job 与触发器，Quartz 到点后从 DI 取实例。作业内部用 `DatabaseContext` 取仓储，与 Web 端是同一套写法。

### 7.7 测试

`samples/TaskBoard/Tests`（13 个用例）只测领域层：

- `ProjectTests`：创建规范化字段并产生事件、空编码/空名称抛异常、改名事件、同名不发事件、归档状态与事件、重复归档抛异常、归档后改名抛异常；
- `TaskItemTests`：创建默认 `Todo` 且 `DueDate` 转 UTC、`projectId = 0` 抛异常、完成置 `Done` 并产生事件、重复完成抛异常、完成后 `Start` 抛异常。

```bash
dotnet test samples/TaskBoard/Tests/Tests.csproj
```

聚合测试的价值在于：这些不变量是 UI、Worker、批量脚本共用的，任何入口都绕不过去。

### 7.8 新增一个聚合的清单

1. `Domain/<聚合>/{实体, 状态枚举, 仓储接口, 事件}`：实体继承 `Entity<int>` + `IFullAuditedObject`，把不变量写进方法；
2. `Infrastructure/Repositories/<聚合>Repository.cs`：继承 `Repository<int, 实体>`，SQL 带 `IsDeleted = 0`；
3. `Application/<聚合>/Dtos` + `I<聚合>AppService`（写方法标 `[UnitOfWork]`）+ 实现；
4. `Infrastructure/TaskBoardServiceCollectionExtensions.cs`：确认 `UseApplication()` 在，仓储会被 `UseRepositories` 自动注册；
5. `Web/Controllers` 加端点，`Tests` 补聚合测试，`docs/samples.md` 与 `CHANGELOG.md` 同步。

## 8. 已知限制与坑

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| 启动即抛「缺少配置节」 | `appsettings.json` 占位值未改或节名拼错 | 对照 [2.1](#21-先补配置) |
| 启动即抛「未设置连接字符串」/ 连接失败 | MySQL/Redis 未启动，或密码未改 | 先本地起服务 |
| 插入成功但库里没数据 | 没走 UOW，也没显式 `SaveChanges` | 见 [延迟持久化](databases.md#4-延迟持久化) |
| 创建后 Id 一直是 0 | 没有在 `InsertAsync` 后 `SaveChangesAsync` | 示例在 `CreateAsync` 里显式保存 |
| 缓存与数据库不一致 | 只删了 Command 侧缓存，Query 侧仍读到旧值 | 示例两套上下文共用一个 Redis 库；真实项目建议读写同库或双删 |
| 定时任务不执行 | `UseScheduler()` 未调用，或作业类不是 public/不可实例化 | 检查 `[Scheduled]`、构造函数依赖是否可解析 |
| 进程重启后错过的任务不补 | Quartz 内存 JobStore | 需要补跑时换持久化 Store |
| 示例无鉴权 | `HttpContextCurrentUser` 直接读 Claims | 接入认证后 `OwnerId`/`CreatorId` 才有真实值，未登录时写 `anonymous` |

相关文档：[快速开始](getting-started.md) · [工作单元与自动事务](unit-of-work.md) · [领域事件](domain-events.md) · [审计字段](auditing.md) · [Redis](redis.md) · [定时任务](scheduler.md) · [设计与落差](design-notes.md)
