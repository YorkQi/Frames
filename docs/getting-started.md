# 快速开始

本文从空工程开始，搭出一个能被 Frames 正确装配、能读写数据库的最小应用。每一步都给出「验证点」，出问题时按 [常见启动错误](#常见启动错误) 排查。

## 1. 环境要求

| 项 | 要求 | 说明 |
| --- | --- | --- |
| .NET SDK | 8.0 | `Frame.Databases` 仅提供 `net8.0` 目标；其余框架程序集为 `netstandard2.1` |
| 数据库 | MySQL 8.x | 默认走 `Pomelo.EntityFrameworkCore.MySql`；未配置方言时 `Dialect` 固定为 `MySqlDialect` |
| IDE | VS 2022 / Rider / VS Code | 解决方案文件 `Frames.sln` |
| 可选 | Redis 6+ | 仅在 `UseLock(LockType.Redis)` 或 `UseRedisDatabase<T>` 时需要 |

框架目前**没有发布 NuGet 包**，使用方式是以项目引用（`ProjectReference`）接入 `src/` 下的程序集。

## 2. 建工程与引用

```bash
dotnet new web -n MyApp
```

在 `MyApp.csproj` 中引用需要的框架程序集（按需裁剪）：

```xml
<ItemGroup>
  <ProjectReference Include="..\Frames\src\Frame.Core\Frame.Core.csproj" />
  <ProjectReference Include="..\Frames\src\Frame.Databases\Frame.Databases.csproj" />
  <ProjectReference Include="..\Frames\src\Frame.Locks\Frame.Locks.csproj" />
  <ProjectReference Include="..\Frames\src\Frame.EventBus\Frame.EventBus.csproj" />
</ItemGroup>
```

> 只需要契约（实体、仓储接口、UOW）时，仅引用 `Frame.Core` 即可。

## 3. 配置连接串

`Program.cs` 从配置读取连接串数组，支持一主多从的负载策略。`appsettings.json`：

```json
{
  "DBConnections": {
    "Command": [ "Server=127.0.0.1;Port=3306;Database=app;User=root;Password=123456;" ],
    "Query":   [ "Server=127.0.0.1;Port=3306;Database=app;User=readonly;Password=123456;" ]
  },
  "RedisDBConnections": {
    "Command": [ "127.0.0.1:6379" ],
    "Query":   [ "127.0.0.1:6379" ]
  }
}
```

- `Command`：写库连接（主库）。
- `Query`：读库连接（可配多个只读副本，由 `ConnectionStringStrategy` 选一个）。
- Redis 段只有启用 Redis 模块时才需要。

## 4. 定义数据库上下文

`DatabaseContext` 是「一次请求内共享同一个 `IDbContext`」的入口，读写分离时通常各建一个：

```csharp
using Frame.Databases;

public sealed class CommandDatabaseContext : DatabaseContext { }
public sealed class QueryDatabaseContext : DatabaseContext { }
```

两者都是空子类——作用仅是区分连接串与 DI 标识，行为完全继承自基类。

## 5. 装配服务

```csharp
using Frame.Core;
using Frame.Databases;
using Frame.Databases.Enums;
using Frame.EventBus;
using Frame.Locks;

var builder = WebApplication.CreateBuilder(args);

var command = builder.Configuration.GetRequiredSection("DBConnections:Command").Get<string[]>()
              ?? throw new Exception("未设置连接字符串");
var query   = builder.Configuration.GetRequiredSection("DBConnections:Query").Get<string[]>()
              ?? throw new Exception("未设置连接字符串");

builder.Services.AddFrameService(options =>
{
    options.UseModule();
    options.UseApplication();

    options.UseDatabaseContext<CommandDatabaseContext>(
        new ConnectionStringCollection(command), ConnectionStringStrategy.RoundRobin);
    options.UseDatabaseContext<QueryDatabaseContext>(
        new ConnectionStringCollection(query), ConnectionStringStrategy.Random);

    options.UseLock(LockType.Local);
    options.UseEventBus();
});

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
```

**验证点**：应用能启动且不抛「未设置连接字符串」「连接字符串未设置」。若使用 EF Core 通道，首次解析 `IDbContext` 时才会真正连接数据库（`ServerVersion.AutoDetect` 会同步探测一次）。

## 6. 写第一个实体 / 仓储 / 服务

```csharp
// Domain
[Table("Users")]
public class User : Entity<int>
{
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
}

public interface IUserRepository : IRepository<int, User>
{
    Task<List<User>> QueryByStatusAsync(int status);
}

// Repository
public class UserRepository : Repository<int, User>, IUserRepository
{
    public Task<List<User>> QueryByStatusAsync(int status)
    {
        const string sql = "SELECT * FROM Users WHERE Status = @Status";
        return DBContext.QueryAsync<User>(sql, new DbParameters().Add("Status", status));
    }
}

// Application
public interface IUserService : IApplicationService
{
    Task<User> GetAsync(int id);
    Task<List<User>> ListAsync(int status);
}

public class UserService : IUserService
{
    private readonly CommandDatabaseContext _db;
    private readonly QueryDatabaseContext _query;

    public UserService(CommandDatabaseContext db, QueryDatabaseContext query)
    {
        _db = db;
        _query = query;
    }

    public Task<User> GetAsync(int id) =>
        _db.GetRepository<IUserRepository>().GetAsync(id);

    public Task<List<User>> ListAsync(int status) =>
        _query.GetRepository<IUserRepository>().QueryByStatusAsync(status);
}
```

`UseApplication()` 会把 `UserService` 以其**第一个** `IApplicationService` 派生接口注册为 `Scoped`，所以控制器注入 `IUserService` 即可。

**验证点**：`GetRepository<IUserRepository>()` 能解析（否则说明仓储没被 `UseDatabaseContext` 扫到，见下文）。

## 7. 控制器

```csharp
[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly IUserService _service;
    public UserController(IUserService service) => _service = service;

    [HttpGet("{id:int}")]
    public Task<User> Get(int id) => _service.GetAsync(id);
}
```

## 8. 写入数据（关键）

EF Core 通道的写操作是**延迟持久化**：`InsertAsync` / `UpdateAsync` / `DeleteAsync` 只把实体挂到变更追踪器上并返回 `0`，不会立即执行 SQL。

**推荐**：在应用服务方法上加 `[UnitOfWork]`。UOW 完成时会自动 `BeginTransactionAsync` → `SaveChangesAsync` → `CommitAsync`，方法内不需要任何提交代码：

```csharp
[UnitOfWork]
public async Task RenameAsync(int id, string name, CancellationToken ct = default)
{
    var repo = _db.GetRepository<IUserRepository>();
    var user = await repo.GetAsync(id, ct);
    user.Name = name;
    await repo.UpdateAsync(user, ct);      // 只改追踪状态
}                                          // ← 方法返回时自动落库并提交
```

没有 UOW（例如脚本、后台任务里单独调仓储）时，必须自己提交。`DatabaseContext` 没有暴露 `SaveChanges`，在业务仓储里加一个转发方法：

```csharp
// Domain
public interface IUserRepository : IRepository<int, User>
{
    Task<List<User>> QueryByStatusAsync(int status);
    Task<int> SaveAsync(CancellationToken ct = default);   // ← 新增
}

// Repository
public class UserRepository : Repository<int, User>, IUserRepository
{
    public Task<int> SaveAsync(CancellationToken ct = default) => DBContext.SaveChangesAsync(ct);
    // ...
}
```

```csharp
var repo = _db.GetRepository<IUserRepository>();
await repo.InsertAsync(new User { Name = "a", Status = 1 });
await repo.SaveAsync();          // 没有 UOW 时，少了这句数据不会落库
```

`DeleteBatchAsync` 也走延迟持久化（逐个 `Remove`），由同一次 `SaveChanges` 提交。

> 自动提交的触发点是 `DatabaseContext.GetRepository<T>()`：它会把自己挂到当前 UOW，注册「收集事件 → 事务 → SaveChanges → Commit」的完成处理器。因此**只解析了 `DatabaseContext` 却没调 `GetRepository` 时不会提交**。详见 [databases.md](databases.md#4-延迟持久化) 与 [工作单元与自动事务](unit-of-work.md#4-与数据库的集成)。

## 9. 运行

```bash
dotnet build Frames.sln
dotnet run --project MyApp
dotnet test Test/TestUnit        # 只跑单元测试，不需要数据库
```

## 常见启动错误

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| `System.Exception: 未设置连接字符串` | `appsettings.json` 缺少 `DBConnections:Command` / `:Query` | 按第 3 节补配置；示例的 `appsettings.json` 已有占位值，但连接串里的密码/地址必须改成真实值 |
| `InvalidOperationException: 连接字符串未设置。请先设置 ConnectionStringAccessor。` | 直接 `new DbContext(...)` 或绕过了 `DatabaseContext` | 通过 `DatabaseContext.GetRepository<T>()` 获取仓储，或自行注入 `DbContextConnectionStringAccessor` |
| `InvalidOperationException: IDbContext 不可用。请确保通过 DatabaseContext 获取仓储实例，或在构造函数中显式注入 IDbContext。` | 仓储实例不是从 `DatabaseContext` 作用域里解析的（`DbContextScope.Current` 为空） | 用 `GetRepository<T>()`，或给仓储构造函数注入 `IDbContext` |
| `InvalidOperationException: 没有可用的连接字符串` | `ConnectionStringCollection` 为空 | 检查配置绑定是否拿到数组 |
| 插入/更新返回 `0` 且库里没数据 | 延迟持久化：没在 UOW 内，或 UOW 内没调 `GetRepository` | 见第 8 节 |
| `ArgumentNullException (Parameter 'options')` | `UseLock(LockType.Redis)` 未传 `LockOptions` | `new LockOptions(redisConnectionStrings)` |
| 仓储解析失败 `No service for type ...` | 仓储类是 `internal`/抽象/泛型开放类型，或没实现 `IRepository` 派生接口 | 改为 `public` 具体类并实现接口；注册发生在 `UseDatabaseContext` 内部 |
| `IUserService` 解析失败 | 类没有实现 `IApplicationService` 派生接口，或接口不是第一个匹配项 | 确认继承 `IApplicationService`；一个类只保留一个应用服务接口 |
| 事件 `Push` 后 Handler 没执行 | Handler 非 `public`、事件实际类型不是 `IEvent`、Handler 泛型参数与事件具体类型不一致 | 见 [eventbus.md](eventbus.md#4-投递规则) |
| 定时任务不执行 | `UseScheduler` 被注释、类未实现 `ISchedulerJob`、Cron 非法、`[Scheduled]` 特性所在的程序集未被发现 | 见 [scheduler.md](scheduler.md#6-排查清单) |

## 下一步

- 理解装配细节：[整体架构](architecture.md)
- 掌握数据访问：[数据库访问](databases.md)
- 先看现状再选型：[设计说明与已知差距](design-notes.md)
