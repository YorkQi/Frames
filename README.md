# Frames — DDD 应用框架

Frames 是一个基于 **DDD（领域驱动设计）** 的 .NET 应用框架，提供模块化、可扩展的基础设施，帮助快速搭建分层架构的应用。

## 模块总览

```
Frame.Core        ← 核心：DI 容器、仓储接口、实体接口、模块扫描、AOP 增强
Frame.Locks       ← 分布式锁：Redis（RedLock） / 本地锁（Monitor）
Frame.EventBus    ← 事件总线：Channel 驱动的异步事件处理
Frame.Scheduler   ← 定时任务：NCrontab 解析 + Quartz 调度
Frame.Databases   ← 数据库：EF Core（变更追踪）+ Dapper（原生 SQL），双引擎
Frame.Redis       ← 缓存：StackExchange.Redis，String/List/Set/Hash/SortedSet
```

**依赖关系**：`Frame.Core` 为所有模块的基础依赖，各功能模块相互独立。

| 模块 | 目标框架 | 核心依赖 |
|------|----------|----------|
| Frame.Core | netstandard2.1 | DI.Abstractions, Logging.Abstractions |
| Frame.Locks | netstandard2.1 | RedLock.net (Redis), Frame.Core |
| Frame.EventBus | netstandard2.1 | System.Threading.Channels, Frame.Core |
| Frame.Scheduler | netstandard2.1 | NCrontab, Quartz.AspNetCore, Frame.Core |
| Frame.Databases | net8.0 | EF Core 8, Dapper, Pomelo.MySql, Frame.Core |
| Frame.Redis | netstandard2.1 | StackExchange.Redis, Frame.Core |

---

## 快速开始

### 1. 入口 —— AddFrameService

所有模块通过 `AddFrameService` 统一注册：

```csharp
using Frame.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFrameService(option =>
{
    // 在这里配置各模块
});
```

### 2. 模块自动注册 —— UseModule

标记 `[ServiceModule]` 的类会被自动扫描并注册到 DI 容器：

```csharp
using Frame.Core.Modules;
using Microsoft.Extensions.DependencyInjection;

// 注册为 Scoped
[ServiceModule(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    // ...
}
```

```csharp
builder.Services.AddFrameService(option =>
{
    option.UseModule();   // 自动扫描并注册所有 [ServiceModule] 标记的类
});
```

---

## 模块详解

### Frame.Databases — 数据访问

一体化数据库上下文，内置 **EF Core（实体 CRUD + 变更追踪）** 和 **Dapper（原生 SQL）** 双引擎。每次请求复用同一个内部 Scope，保证 EF Core 变更追踪一致性。

#### 注册

```csharp
using Frame.Databases;

builder.Services.AddFrameService(option =>
{
    option.UseDatabaseContext<CommandDatabaseContext>(
        new ConnectionStringCollection("Server=...;Database=...;User Id=...;Password=..."));

    option.UseDatabaseContext<QueryDatabaseContext>(
        new ConnectionStringCollection("Server=...;Database=...;User Id=...;Password=..."));
});

// 或从 appsettings.json 读取 string[] 后使用 C# 12 集合表达式：
// string[] querys = config.GetSection("DBConnections:Query").Get<string[]>()!;
// option.UseDatabaseContext<CommandDatabaseContext>([.. querys]);
```

`DatabaseContext` 实现 `IDisposable` / `IAsyncDisposable`，生命周期为 **Scoped**，请求结束自动释放。

#### 实体定义

```csharp
using Frame.Core.Entities;

public class User : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserSex Sex { get; set; }
    public string ProfilePicture { get; set; } = string.Empty;
    public long CreateTime { get; set; }
}
```

#### 自定义仓储

```csharp
// 接口 — Domain 层
using Frame.Core.Repositories;

public interface IUserRepository : IRepository<int, User>
{
    Task<IEnumerable<User>> QueryAsync();
    Task<IEnumerable<User>> QueryByDapperAsync(string name);
}

// 实现 — Infrastructure 层
using Frame.Databases.Repositories;

public class UserRepository : Repository<int, User>, IUserRepository
{
    // Dapper 原生 SQL
    public Task<IEnumerable<User>> QueryAsync()
    {
        var sql = "SELECT * FROM `Users` WHERE Id > 1;";
        return DBContext.QueryAsync<User>(sql);
    }

    // 参数化查询
    public Task<IEnumerable<User>> QueryByDapperAsync(string name)
    {
        return DBContext.QueryAsync<User>(
            "SELECT * FROM `Users` WHERE Name LIKE @Name",
            new { Name = $"%{name}%" });
    }
}
```

Repository 会被自动扫描注册为 Scoped，无需手动配置。

#### DbParameters — 框架原生参数

替代匿名对象的统一参数类型，支持流式 API 条件追加、Output 参数读回。匿名对象写法仍完全兼容。

```csharp
using Frame.Databases;

// 动态条件：AddIf 按需追加，替代多个 if-else
var p = new DbParameters()
    .Add("Keyword", $"%{keyword}%")
    .AddIf(status.HasValue, "Status", status.Value)
    .AddIf(tagIds?.Any() == true, "TagIds", tagIds)
    .AddOutput("TotalCount", DbType.Int32);

var users = await DBContext.QueryAsync<User>("sp_Search", p,
    commandType: CommandType.StoredProcedure);

var total = p.Get<int>("TotalCount"); // Output 参数读回
```

#### QueryPaged — 分页查询

一次往返完成 COUNT + 数据查询，返回 `PageResult<T>`（`Items` + `Count`）。

```csharp
var result = await DBContext.QueryPagedAsync<User>(
    "SELECT * FROM `Users` WHERE Status = @Status",
    new DbParameters(new { Status = 1 }),
    page: 1, size: 20);

// result.Items  → 当前页数据（IEnumerable<User>）
// result.Count  → 符合条件的总记录数（long）
```

#### 使用 DatabaseContext

```csharp
public class UserService
{
    private readonly CommandDatabaseContext _command;

    public UserService(CommandDatabaseContext command)
    {
        _command = command;
    }

    public async Task DoWork()
    {
        // 获取自定义仓储
        var repo = _command.GetRepository<IUserRepository>();
        var users = await repo.QueryByDapperAsync("York");

        // 获取通用仓储（基础 CRUD）
        var baseRepo = _command.GetRepository<int, User>();
        var user = await baseRepo.GetAsync(1);
        user.Name = "Updated";
        await baseRepo.UpdateAsync(user);

        // 批量操作
        await baseRepo.InsertBatchAsync(new List<User> { /*...*/ });
        await baseRepo.DeleteBatchAsync(new List<int> { 1, 2, 3 });
    }
}
```

#### IDbContext 能力一览

| 类别 | 方法 |
|------|------|
| **底层** | `GetDbConnection()` / `GetDbConnectionAsync()` |
| **事务** | `BeginTransaction()` / `Commit()` / `Rollback()` |
| **查询** | `Query<T>()` / `QueryFirst<T>()` / `QuerySingle<T>()` / `QueryMultiple()` / `QueryPaged<T>()` |
| **写操作** | `Execute()` / `ExecuteScalar<T>()` |
| **EF Core** | `Find<T>()` / `Queryable<T>()` / `Entry<T>()` / `Attach()` / `Detach()` |

---

### Frame.Locks — 分布式锁

支持 **本地锁**（`Monitor`）和 **Redis 分布式锁**（RedLock 算法），通过统一接口 `ILock` 使用。

#### 注册

```csharp
using Frame.Locks;
using Frame.Locks.Enums;

builder.Services.AddFrameService(option =>
{
    // 本地锁
    option.UseLock(LockType.Local);

    // Redis 分布式锁
    option.UseLock(LockType.Redis, new LockOptions(new[]
    {
        "127.0.0.1:6379,password=xxx,connectTimeout=1000"
    }));
});
```

`ILockFactory` 注册为 **Singleton**，`ILock` 实例通过 `using` 管理生命周期，实现 `IDisposable` / `IAsyncDisposable`。

#### 使用

```csharp
using Frame.Locks;

public class OrderService
{
    private readonly ILockFactory _lockFactory;

    public OrderService(ILockFactory lockFactory)
    {
        _lockFactory = lockFactory;
    }

    public async Task ProcessOrder(int orderId)
    {
        // 同步获取
        using (var @lock = _lockFactory.CreateLock($"order:{orderId}"))
        {
            if (@lock.IsAcquired)
            {
                // 处理业务...
            }
        } // 自动释放

        // 异步获取，自定义过期时间
        await using (var @lock = await _lockFactory.CreateLockAsync(
            $"order:{orderId}", TimeSpan.FromSeconds(60)))
        {
            if (@lock.IsAcquired)
            {
                // 处理业务...
            }
        }
    }
}
```

---

### Frame.EventBus — 事件总线

基于 `Channel<T>` 的异步事件处理，事件入队后由后台 `BackgroundService` 消费，每个事件处理器在独立 Scope 中执行。

#### 注册

```csharp
using Frame.EventBus;

builder.Services.AddFrameService(option =>
{
    option.UseEventBus();
});
```

#### 定义事件和处理

```csharp
// 事件参数
using Frame.EventBus;

public class TestEvent : IEvent
{
    public string Name { get; set; } = string.Empty;
}

// 事件处理器
public class TestEventHandler : IEventHandler<TestEvent>
{
    public Task ExecuteAsync(TestEvent param)
    {
        Console.WriteLine(param.Name);
        return Task.CompletedTask;
    }
}
```

#### 发布事件

```csharp
public class HomeController : Controller
{
    private readonly IEventBus _eventBus;

    public HomeController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task<IActionResult> Index()
    {
        await _eventBus.Push(new TestEvent { Name = "Hello" });
        return View();
    }
}
```

事件处理器自动扫描注册为 Scoped，Handler 内的 `DatabaseContext` 等 Scoped 服务在独立 Scope 内正常工作。

---

### Frame.Scheduler — 定时任务

基于 **NCrontab** 解析 Cron 表达式（支持 5 段/6 段式），**Quartz** 执行调度。

| 格式 | 结构 | 示例 |
|------|------|------|
| 5 段式 | `分 时 日 月 周` | `0 0 * * *` |
| 6 段式 | `秒 分 时 日 月 周` | `0/5 * * * * *` |

#### 注册

支持两种配置方式，可同时使用（去重合并）：

```csharp
using Frame.Scheduler;

builder.Services.AddFrameService(options =>
{
    // 方式一：特性扫描 — 自动发现标注了 [Scheduled] 的 ISchedulerJob 实现
    options.UseScheduler();

    // 方式二：手动配置 — 显式指定作业类型名称和 Cron 表达式
    options.UseScheduler(new List<JobSchedule>
    {
        new("Job.Tasks.TestScheduler", "0/5 * * * * *", "每 5 秒执行"),
        new("Job.Tasks.Test2Scheduler", "0 0 8 * * ?", "每天 8 点执行"),
    });
});
```

#### 定义任务

```csharp
using Frame.Scheduler;

[Scheduled("0/5 * * * * *")]  // 每 5 秒执行
public class Test3Scheduler : ISchedulerJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"执行时间: {DateTime.Now}");
        return Task.CompletedTask;
    }
}
```

---

### Frame.Redis — 分布式缓存

基于 StackExchange.Redis，提供 String / List / Set / Hash / SortedSet / Pub-Sub 全覆盖。

#### 注册

```csharp
using Frame.Redis;

builder.Services.AddFrameService(option =>
{
    option.UseRedisDatabase<CommandRedisContext>(
        new RedisConnectionStringCollection("127.0.0.1:6379,password=xxx,connectTimeout=1000"));

    option.UseRedisDatabase<QueryRedisContext>(
        new RedisConnectionStringCollection("127.0.0.1:6379,password=xxx,connectTimeout=1000"));
});
```

`RedisContext` 注册为 **Scoped**，实现 `IDisposable` / `IAsyncDisposable`。

#### 使用

```csharp
public class CacheService
{
    private readonly CommandRedisContext _redis;

    public CacheService(CommandRedisContext redis)
    {
        _redis = redis;
    }

    public async Task DoWork()
    {
        var db = _redis.GetDbContext();

        // String
        await db.SetAsync("key", "value", TimeSpan.FromMinutes(10));
        var val = await db.GetAsync("key");

        // Hash
        await db.HashSetAsync("user:1", "name", "York");
        var all = await db.HashGetAllAsync("user:1");

        // List
        await db.ListRightPushAsync("queue", "item1");

        // Set
        await db.SetAddAsync("tags", "dotnet");

        // SortedSet
        await db.SortedSetAddAsync("scores", "player1", 100);

        // Pub-Sub
        await db.PublishAsync("channel", "message");
    }
}
```

---

## 项目结构示例

```
samples/Webs/
├── Domain/            # 领域层 — 实体、仓储接口
│   └── Users/
│       ├── User.cs              → IEntity<int>
│       └── IUserRepository.cs   → IRepository<int, User>
├── Application/       # 应用层 — 应用服务
│   └── UserService.cs          → 注入 DatabaseContext
├── Infrastructure/    # 基础设施层 — DatabaseContext 子类
│   └── DatabaseContexts/
│       ├── CommandDatabaseContext.cs
│       └── QueryDatabaseContext.cs
├── Repository/        # 仓储实现层
│   └── UserRepository.cs       → Repository<int, User>
└── Web/               # 表现层 — ASP.NET Core
    ├── Controllers/
    ├── EventBus/               → IEventHandler<T> 实现
    └── Program.cs              → 注册入口
```

---

## License

MIT
