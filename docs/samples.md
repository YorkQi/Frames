# 示例工程

仓库里有两个示例：

- `samples/Webs` — ASP.NET Core Web API 的完整 DDD 分层示例（5 个项目）；
- `samples/Scheduler` — 控制台/Web SDK 的定时任务示例（1 个项目）。

示例同时充当「框架能跑起来」的验收用例，但它**不是**最佳实践模板：里面有若干与框架现状相关的坑，见第 5 节。

## 1. 项目结构与引用链

```
samples/Webs/
├── Domain/          # 实体、仓储接口、应用服务接口、枚举
├── Application/     # 应用服务实现
├── Repository/      # 仓储实现（Dapper/EF Core）
├── Infrastructure/  # DatabaseContext / RedisContext 子类
└── Web/             # Program.cs、控制器、过滤器、事件

samples/Scheduler/
└── Job/             # Program.cs + 三个作业
```

引用方向（箭头表示「依赖」）：

```
Web ──▶ Application ──▶ Infrastructure ──▶ Repository ──▶ Domain ──▶ Frame.Core
                                          Repository ──▶ Frame.Databases
                          Infrastructure ──▶ Frame.EventBus
                          Infrastructure ──▶ Frame.Locks
                          Infrastructure ──▶ Frame.Redis

Job ──▶ Frame.Scheduler
```

要点：

- `Web` **只**引用 `Application`，不直接引用 `Repository`/`Infrastructure`——依赖倒置的示范；
- `Domain` 不引用任何 `Frame.*` 之外的第三方包；
- `Infrastructure` 是「框架装配层」，放 `DatabaseContext`、`RedisContext` 子类；
- 两个示例 TFM 都是 `net8.0`。

## 2. 运行 Web 示例

### 2.1 先补配置

`Web/appsettings.json` 当前**只有** `Logging` 和 `AllowedHosts`。直接运行会在启动时抛 `Exception("未设置连接字符串")`。补上：

```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*",
  "DBConnections": {
    "Query":   [ "server=127.0.0.1;port=3306;database=frames;user=root;password=root" ],
    "Command": [ "server=127.0.0.1;port=3306;database=frames;user=root;password=root" ]
  },
  "RedisDBConnections": {
    "Query":   [ "127.0.0.1:6379" ],
    "Command": [ "127.0.0.1:6379" ]
  }
}
```

数据库需要存在 `Users` 表（`User` 实体标了 `[Table("Users")]`）：

```sql
CREATE TABLE Users (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  Name VARCHAR(100),
  Sex INT,
  ProfilePicture VARCHAR(500),
  Status INT,
  CreateTime BIGINT
);
```

### 2.2 启动

```bash
dotnet run --project samples/Webs/Web
```

访问 `GET /Home/Index` 会依次触发控制器里的演示代码（见第 4 节）。

### 2.3 装配代码

```csharp
builder.Services.AddFrameService(options =>
{
    options.UseApplication();
    options.UseModule();
    options.UseDatabaseContext<CommandDatabaseContext>([.. querys]);
    options.UseDatabaseContext<QueryDatabaseContext>([.. commands]);   // ← 传反了，见 5.1
    options.UseLock(LockType.Local);
    options.UseEventBus();
    // options.UseRedisDatabase<CommandRedisContext>([.. redisCommands]);
});
```

给 `Web` 服务添加了两个 `DatabaseContext` 子类（`CommandDatabaseContext`、`QueryDatabaseContext`），都只是空的继承类，用于区分读写连接串。

## 3. 各层代码

### 3.1 Domain

```csharp
[Table("Users")]
public class User : Entity<int>
{
    public string Name { get; set; }
    public UserSex Sex { get; set; }
    public string? ProfilePicture { get; set; }
    public UserStatus Status { get; set; }
    public long CreateTime { get; set; }
}
```

仓储接口暴露「框架能力 + 业务查询」：

```csharp
public interface IUserRepository : IRepository<int, User>
{
    Task<List<User>> QueryAsync();
    Task<List<User>> QueryByConditionAsync(string? name, UserSex? sex, int? status);
    Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size);
}
```

### 3.2 Repository

```csharp
public class UserRepository : Repository<int, User>, IUserRepository
{
    public Task<List<User>> QueryAsync()
        => DBContext.QueryAsync<User>("SELECT * FROM Users");

    public Task<List<User>> QueryByConditionAsync(string? name, UserSex? sex, int? status)
    {
        var sql = "SELECT * FROM Users WHERE 1=1";
        var p = new DbParameters();
        if (!string.IsNullOrWhiteSpace(name)) { sql += " AND Name LIKE @name"; p.Add("name", $"%{name}%"); }
        if (sex.HasValue)   { sql += " AND Sex = @sex";   p.Add("sex", (int)sex.Value); }
        if (status.HasValue){ sql += " AND Status = @status"; p.Add("status", status.Value); }
        return DBContext.QueryAsync<User>(sql, p);
    }

    public Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size)
    {
        var sql = "SELECT * FROM Users WHERE 1=1";
        var p = new DbParameters();
        if (!string.IsNullOrWhiteSpace(name)) { sql += " AND Name LIKE @name"; p.Add("name", $"%{name}%"); }
        sql += " ORDER BY Id DESC";
        return DBContext.QueryPagedAsync<User>(sql, p, page, size);
    }
}
```

注意 `DBContext` 来自 `Repository<,>` 的 `protected` 属性，它优先返回构造注入的实例，否则取 `DbContextScope.Current`——在 `DatabaseContext.GetRepository<T>()` 拿到的仓储里，它一定指向该 `DatabaseContext` 子 Scope 里的实例。

### 3.3 Application

```csharp
public class UserService : IUserService        // 注意：没有继承 ApplicationService
{
    private readonly CommandDatabaseContext _db;
    public UserService(CommandDatabaseContext db) => _db = db;

    public Task<List<User>> GetAsync()
        => _db.GetRepository<IUserRepository>().QueryAsync();
}
```

示例**没有用** `ApplicationService` 基类，也没有 `BeginUnitOfWork`/`CompleteUnitOfWorkAsync`，所以示例里的 UOW 全程未启用。

### 3.4 Web

- `HomeController` 是「把所有 API 都调用一遍」的验收控制器：锁、仓储 CRUD、批量、分页、事件发布、应用服务；
- `Filters/ExceptionFilter` 只处理 `BusinessException`，把 `Code`/`Message` 包成 `ExceptionResult`，HTTP 状态取 `StateCode ?? 400`；
- `Filters/ModelValidationFilter.cs` 里定义的其实是 `ModelValidationAttribute : ActionFilterAttribute`，模型校验失败返回 400 + 错误列表；
- `EventBus/TestEvent` + `TestEventHandler` 演示最小事件；
- 文件底部还有一组与框架无关的业务枚举（`ServiceCode`/`PlatformType`/`ServiceType`），是历史遗留，可忽略。

## 4. 一次请求发生了什么

以 `GET /Home/Index` 为例：

```
HomeController (Scoped)
  ├─ IEventBus          → LocalEventBus (Singleton) + HostedService 消费
  ├─ CommandDatabaseContext (Scoped)
  │    └─ 首次 GetRepository<T>() 时创建子 Scope：
  │         ├─ 写 ConnectionStringAccessor
  │         ├─ 解析 IDbContext（DbContext，Dialect=MySql）
  │         └─ DbContextScope.Set(...)
  ├─ IUserService       → UserService (Scoped) → CommandDatabaseContext (同一个 Scoped 实例)
  └─ ILockFactory       → LocalLockFactory (Singleton)
```

事件发布后由 `EventBusHostService` 的后台循环异步处理，控制器不等待。

## 5. 示例里的坑

### 5.1 连接串传反

```csharp
options.UseDatabaseContext<CommandDatabaseContext>([.. querys]);   // Command 拿到 Query 串
options.UseDatabaseContext<QueryDatabaseContext>([.. commands]);   // Query 拿到 Command 串
```

两个连接串相同时无影响；读写分离时会写错库。

### 5.2 缺配置节

`appsettings.json` 没有 `DBConnections`，`Program.cs` 用 `GetRequiredSection(...).Get<string[]>()!`，缺失即抛 `Exception("未设置连接字符串")`。

### 5.3 写了不落库

`InsertAsync`/`UpdateAsync`/`DeleteAsync` 只做变更追踪并返回 `0`。示例里没有调用 `SaveChangesAsync`，所以「插入」在数据库里看不到——除非你在仓储里补了 `SaveAsync`。

### 5.4 Redis 被注释

`UseRedisDatabase` 两行都注释掉了，`CommandRedisContext` 从未注册；控制器里也没有用到 Redis。

### 5.5 大量无意义批量

`InsertBatchAsync` 塞了 10000 条 `User` 且不提交，只是让 `ChangeTracker` 变大；不要照抄。

### 5.6 控制器里的业务枚举

`ServiceCode` 之类与框架无关，属于示例作者的业务残留；`[Flags]` 组合用 `<< 0x20` 这类写法也不建议照抄。

## 6. 调度示例

```csharp
builder.Services.AddFrameService(options =>
{
    options.UseModule();
    options.UseApplication();
    // options.UseScheduler();      // ← 被注释：不扫描 [Scheduled]

    List<JobSchedule> jobs =
    [
        new("TestScheduler",  typeof(Job.Tasks.TestScheduler).FullName!,  "0/5 * * * * *"),
        new("Test2Scheduler", typeof(Job.Tasks.Test2Scheduler).FullName!, "0/5 * * * * *"),
    ];
    options.UseScheduler(jobs);
});
```

- 两个作业每 5 秒打印一次；
- `Test3Scheduler` 标了 `[Scheduled]`，但因为 `UseScheduler()` 无参重载被注释，**不会注册**；
- `Job` 项目是 `Microsoft.NET.Sdk.Web`，靠 `WebApplication.CreateBuilder` 提供的 Host 驱动 `SchedulerHostedService`。换成 `Microsoft.NET.Sdk` + `Host.CreateApplicationBuilder` 也可以，但必须有 `IHostedService` 的宿主。

## 7. 如果要从示例改成真实项目

1. 把 `appsettings.json` 的配置节补全，或改用 `IConfiguration` 绑定到强类型 `DatabaseOptions`；
2. 在仓储里加 `SaveAsync`（或给 `DatabaseContext` 加 `SaveChangesAsync`），并在应用服务里显式调用；
3. `UserService` 继承 `ApplicationService`，需要事务时用 `BeginUnitOfWork`/`CompleteUnitOfWorkAsync`；
4. 删掉控制器里的验收代码，按聚合拆控制器；
5. 不要把 `Infrastructure` 的项目引用暴露给 `Web`；`Web` 只应看到 `Application` 与 `Domain`。
