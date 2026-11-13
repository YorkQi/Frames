# Frames

一个分层、模块化的 .NET 应用框架，按 DDD（领域驱动设计）的经典四层组织代码，把「程序集发现 → 依赖注入 → 数据访问 → 事务/工作单元 → 横切能力（锁、事件、调度、缓存）」收敛成一组 `AddFrameService(options => ...)` 扩展方法。

- **约定优于配置**：入口程序集递归发现业务程序集，按标记接口/特性自动注册服务；
- **双通道数据访问**：EF Core 负责实体变更追踪与延迟持久化，Dapper 负责手写 SQL、批量删除与分页查询；
- **可替换的横切能力**：本地/Redis 分布式锁、进程内事件总线（通道 + 重试）、Quartz 定时任务、Redis 数据结构访问层；
- **小核心**：`Frame.Core` 只依赖 `Microsoft.Extensions.*` 抽象，其余模块按需引用。

> ⚠️ 本仓库仍在演进中。部分设计（`[UnitOfWork]` 自动事务、领域事件分发、审计字段、模块生命周期钩子）目前只有骨架或尚未接线，使用前请先阅读 [设计说明与已知差距](docs/design-notes.md)。

---

## 模块一览

| 程序集 | 目标框架 | 职责 | 主要扩展点 |
| --- | --- | --- | --- |
| `Frame.Core` | netstandard2.1 | 实体基类、仓储契约、工作单元、模块系统、DI 引导 | `AddFrameService` / `UseModule` / `UseApplication` |
| `Frame.Databases` | net8.0 | EF Core + Dapper 的 `IDbContext`、仓储实现、分页与方言、`DbParameters` | `UseDatabaseContext<T>` |
| `Frame.Locks` | netstandard2.1 | 分布式/本地互斥锁（`Monitor` 或 RedLock） | `UseLock` |
| `Frame.EventBus` | netstandard2.1 | 进程内事件总线（`Channel` + 指数退避重试） | `UseEventBus` |
| `Frame.Scheduler` | netstandard2.1 | 基于 Quartz 的定时任务，支持 `[Scheduled]` 与代码注册 | `UseScheduler` |
| `Frame.Redis` | netstandard2.1 | `StackExchange.Redis` 的字符串/列表/集合/哈希/有序集合访问层 | `UseRedisDatabase<T>` |

依赖关系（箭头表示「引用」）：

```
Frame.Core
   ▲   ▲   ▲   ▲   ▲
   │   │   │   │   └── Frame.Redis
   │   │   │   └────── Frame.Scheduler   (Quartz 3.3.3 / NCrontab 3.4.0)
   │   │   └────────── Frame.EventBus    (Hosting.Abstractions / Channels)
   │   └────────────── Frame.Locks       (RedLock.net 2.3.2)
   └────────────────── Frame.Databases   (EF Core 8 / Pomelo MySQL / Dapper 2)
```

除 `Frame.Databases` 外均为 `netstandard2.1`，可被 .NET Core 3.x 及以上引用；示例与测试工程统一使用 `net8.0`。

---

## 快速开始

### 1. 引用与注册

```csharp
using Frame.Core;                 // AddFrameService
using Frame.Databases;            // UseDatabaseContext / ConnectionStringCollection
using Frame.Databases.Enums;      // ConnectionStringStrategy
using Frame.Locks;                // UseLock / LockType
using Frame.EventBus;             // UseEventBus

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFrameService(options =>
{
    options.UseModule();          // 注册带 [ServiceModule] 的类
    options.UseApplication();     // 注册 IApplicationService 实现
    options.UseDatabaseContext<CommandDatabaseContext>(
        new ConnectionStringCollection(connectionString),
        ConnectionStringStrategy.RoundRobin);
    options.UseLock(LockType.Local);
    options.UseEventBus();
});

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
```

`AddFrameService` 内部顺序：**用户配置 → 模块拓扑（Pre/Configure/Post）→ 统一写入容器 → 基础设施补齐**。业务程序集默认从入口程序集递归发现，需要额外程序集时用 `options.AddAssemblyOf<T>()`。

### 2. 定义实体与仓储

```csharp
[Table("Users")]
public class User : Entity<int>
{
    public string Name { get; set; } = string.Empty;
    public UserSex Sex { get; set; }
    public long CreateTime { get; set; }
}

public interface IUserRepository : IRepository<int, User>
{
    Task<List<User>> QueryByConditionAsync(string? name, UserSex? sex, int? status);
}

public class UserRepository : Repository<int, User>, IUserRepository
{
    public Task<List<User>> QueryByConditionAsync(string? name, UserSex? sex, int? status)
    {
        var sql = "SELECT * FROM Users WHERE 1 = 1";
        var parameters = new DbParameters();
        if (!string.IsNullOrWhiteSpace(name))
        {
            sql += " AND Name LIKE @Name";
            parameters.Add("Name", $"%{name}%");
        }
        if (sex.HasValue)   { sql += " AND Sex = @Sex";       parameters.Add("Sex", (int)sex.Value); }
        if (status.HasValue){ sql += " AND Status = @Status"; parameters.Add("Status", status.Value); }

        return DBContext.QueryAsync<User>(sql, parameters);
    }
}
```

仓储无需手工注册：`UseDatabaseContext` 会扫描并注册所有「实现了 `IRepository` 派生接口的具体类」，生命周期 `Scoped`。

### 3. 使用

```csharp
public class UserService : IApplicationService
{
    private readonly CommandDatabaseContext _db;
    public UserService(CommandDatabaseContext db) => _db = db;

    public Task<User> GetAsync(int id) => _db.GetRepository<IUserRepository>().GetAsync(id);
}
```

> **写入必须落库**：EF Core 通道的 `Insert/Update/Delete` 只做变更追踪并返回 `0`，真正提交依赖 `IDbContext.SaveChangesAsync()`（通过业务仓储的 `DBContext` 属性调用）。当前框架不会在 UOW 完成时自动调用它，详见 [数据库访问](docs/databases.md#4-延迟持久化)。

---

## 文档导航

| 文档 | 内容 |
| --- | --- |
| [文档索引](docs/README.md) | 全部文档的阅读顺序与维护约定 |
| [快速开始](docs/getting-started.md) | 环境要求、最小可运行示例、配置、常见启动错误 |
| [整体架构](docs/architecture.md) | 分层、程序集发现、DI 生命周期、请求链路、模块系统 |
| [核心模块 Core](docs/core.md) | `Entity`、`IRepository`、UOW、`ApplicationService`、分页 DTO |
| [数据库访问](docs/databases.md) | `IDbContext` 双通道、延迟持久化、方言与分页、`DbParameters`、连接串策略 |
| [分布式锁](docs/locks.md) | `ILock`/`ILockFactory`、本地锁与 RedLock 的差异与注意事项 |
| [事件总线](docs/eventbus.md) | `IEvent`/`IEventHandler`、投递模型、重试与丢失语义 |
| [定时任务](docs/scheduler.md) | `[Scheduled]`、`JobSchedule`、Cron 规则、Quartz 存储 |
| [Redis 访问层](docs/redis.md) | 数据结构 API、多路复用器生命周期与已知隐患 |
| [示例工程](docs/samples.md) | `samples/Webs` 五层走读、`samples/Scheduler`、运行步骤 |
| [设计说明与已知差距](docs/design-notes.md) | 设计决策、与代码不一致之处、技术债与修复优先级 |
| [测试说明](docs/testing.md) | 测试组织、覆盖范围、缺口 |
| [贡献指南](CONTRIBUTING.md) | 分支、提交、代码风格、构建与测试 |
| [变更日志](CHANGELOG.md) | 版本记录 |

---

## 项目结构

```
Frames/
├─ src/                         框架源码（6 个程序集）
│  ├─ Frame.Core/               Entities / Repositories / Uow / Modules / Application
│  ├─ Frame.Databases/          IDbContext（EF Core partial + Dapper partial）/ 方言 / 仓储 / DbParameters
│  ├─ Frame.Locks/              Local / Redis 两种 ILockFactory
│  ├─ Frame.EventBus/           IEvent / IEventHandler / LocalEventBus / 宿主服务
│  ├─ Frame.Scheduler/          IFrameScheduler / Quartz 适配 / [Scheduled]
│  └─ Frame.Redis/              IRedisDbContext / RedisContext / 连接串集合
├─ samples/
│  ├─ Webs/                     Web → Application → Infrastructure → Repository → Domain 五层示例
│  └─ Scheduler/Job/            Quartz 定时任务示例（Web SDK 承载宿主）
├─ Test/TestUnit/               xUnit 单元测试
├─ docs/                        设计文档与使用说明
├─ Frames.sln
└─ README.md
```

命名约定：`Frame.*` 为框架程序集，示例业务程序集为 `Domain / Application / Repository / Infrastructure / Web`，依赖方向严格单向（Web 不直接引用 Repository/Databases）。

---

## 环境与版本

| 项 | 要求 |
| --- | --- |
| .NET SDK | 8.0（`Frame.Databases` 与示例均为 `net8.0`） |
| 数据库 | MySQL 8（默认方言 `MySqlDialect`，Pomelo 提供程序） |
| 可选 | Redis 7（分布式锁 / Redis 访问层）；SQL Server、PostgreSQL 有方言实现但未接入注册流程 |
| 测试 | xUnit；`dotnet test Test/TestUnit` |

---

## 当前状态

已可用：程序集发现与 DI 装配、`Entity` 相等性、Dapper SQL/分页、三套方言、`DbParameters`、连接串负载策略、本地/Redis 锁、进程内事件总线、Quartz 调度、Redis 数据结构封装。

尚未接线或存在已知问题：`[UnitOfWork]` 无拦截器、领域事件无分发、模块 `OnApplicationInitializationAsync` 永不执行、审计接口不存在、`RedisDbContext.Dispose` 会关闭全局多路复用器、示例连接串配置缺失。逐条说明与修复建议见 [design-notes.md](docs/design-notes.md)。

---

## License

MIT
