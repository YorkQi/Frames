# Frames

一个分层、模块化的 .NET 应用框架：把「程序集发现 → 依赖注入 → 数据访问 → 工作单元 → 横切能力（锁、事件、调度、缓存）」收敛成一组 `AddFrameService(options => ...)` 扩展方法。

- **约定优于配置**：入口程序集递归发现业务程序集，按标记接口/特性自动注册服务；
- **双通道数据访问**：EF Core 负责变更追踪与延迟持久化，Dapper 负责手写 SQL 与分页；
- **可替换的横切能力**：本地/Redis 锁、进程内事件总线、Quartz 调度、Redis 访问层；
- **小核心**：`Frame.Core` 只依赖 `Microsoft.Extensions.*` 抽象。

> ⚠️ 仍在演进：`[UnitOfWork]` 自动事务、领域事件分发、审计字段、模块生命周期钩子均已可用并有测试，但存在明确边界（无 outbox、Dapper 通道不做审计、示例需手工建表）。动手前先读 [设计说明](docs/design/design-notes.md)。

## 60 秒上手

```csharp
using Frame.Core;              // AddFrameService
using Frame.Databases;         // UseDatabaseContext
using Frame.EventBus;          // UseEventBus

builder.Services.AddFrameService(options =>
{
    options.UseModule();       // [ServiceModule] 标注的类
    options.UseApplication();  // IApplicationService 实现（[UnitOfWork] 会生成代理）
    options.UseDatabaseContext<CommandDatabaseContext>(
        new ConnectionStringCollection(connectionString));
    options.UseEventBus();
    options.UseLock(LockType.Local);
});
```

服务只要在接口上标 `[UnitOfWork]`，方法正常返回时工作单元完成「收集事件 → 开事务 → `SaveChanges` → 提交 → 分发事件」，异常自动回滚：

```csharp
public interface IUserService : IApplicationService
{
    [UnitOfWork]
    Task<int> RenameAsync(int id, string name, CancellationToken cancellationToken = default);
}
```

完整可运行工程、建表 SQL 与排错见 [快速开始](docs/guide/getting-started.md)；按任务的最小改动清单见 [常见任务配方](docs/guide/recipes.md)。

## 程序集

| 程序集 | 目标框架 | 职责 | 扩展点 |
| --- | --- | --- | --- |
| `Frame.Core` | `netstandard2.1` | 实体、仓储契约、UOW、审计契约、模块系统、DI 引导 | `AddFrameService` / `UseModule` / `UseApplication` |
| `Frame.Databases` | `net8.0` | EF Core + Dapper 双通道 `IDbContext`、方言、分页、`DbParameters` | `UseDatabaseContext<T>` |
| `Frame.Locks` | `netstandard2.1` | 本地 `Monitor` 锁 / RedLock 分布式锁 | `UseLock` |
| `Frame.EventBus` | `netstandard2.1` | 进程内事件总线（Channel + 指数退避重试） | `UseEventBus` |
| `Frame.Scheduler` | `netstandard2.1` | Quartz 包装 + `[Scheduled]` | `UseScheduler` |
| `Frame.Redis` | `netstandard2.1` | StackExchange.Redis 的键/串/列表/集合/哈希/有序集合访问层 | `UseRedisDatabase<T>` |

除 `Frame.Databases` 外均为 `netstandard2.1`；示例与测试为 `net8.0`。依赖方向：`Frame.*` 之间只允许依赖 `Frame.Core`，`samples/` 与 `Test/` 不被 `src/` 引用。

## 文档地图

| 我要… | 读这个 |
| --- | --- |
| 跑起来、接进现有项目 | [快速开始](docs/guide/getting-started.md) → [示例工程](docs/guide/samples.md) |
| 做某个具体功能 | [常见任务配方](docs/guide/recipes.md) |
| 搞懂某个机制 | [整体架构](docs/concepts/architecture.md) 或 `docs/concepts/` 下对应页面 |
| 改框架、加模块 | [模块与目录结构](docs/concepts/modules.md) → [设计说明](docs/design/design-notes.md) → [ADR](docs/design/adr/README.md) |
| 确认某能力是否可用 | [设计目标 vs 现状](docs/design/design-notes.md#0-设计目标-vs-现状) |
| 跑测试、提交改动 | [测试](docs/ops/testing.md) → [贡献指南](CONTRIBUTING.md) → [AGENTS.md](AGENTS.md) |

完整索引与维护约定在 [docs/README.md](docs/README.md)。文档由 `scripts/check-docs.ps1` 机械校验（链接、锚点、front matter、事实数字），规则见 [文档一致性检查](docs/ops/doc-consistency.md)。

## 目录

```
Frames/
├─ src/                 6 个框架程序集（Frame.Core / Databases / Locks / EventBus / Scheduler / Redis）
│  └─ */AGENTS.md       各程序集的目录职责与改动约束
├─ samples/             示例：Webs（最小 API）、Scheduler/Job、TaskBoard（多宿主项目集；AGENTS.md 说明运行与引用约束）
├─ Test/                框架单元测试（xUnit；AGENTS.md 说明测试分工）
├─ docs/                guide / concepts / design / ops 四层文档
├─ scripts/             check-docs.ps1（文档一致性）、migrate-docs.ps1（一次性迁移）
├─ .githooks/           pre-commit：提交前跑文档检查
├─ .github/workflows/   docs.yml：CI 兜底
├─ Frames.sln
├─ AGENTS.md            仓库约定（AI 助手 / 新人）
├─ CONTRIBUTING.md      贡献指南
├─ CHANGELOG.md         变更日志
└─ README.md
```

## 环境与状态

| 项 | 要求 |
| --- | --- |
| .NET SDK | 8.0（`Frame.Databases` 与示例为 `net8.0`） |
| 数据库 | MySQL 8（默认方言 `MySqlDialect`） |
| 可选 | Redis 7；SQL Server / PostgreSQL 有方言实现但未接入注册流程 |
| 测试 | `dotnet test Frames.sln`：112 个用例（框架 99 + 示例 13） |

已可用：程序集发现与 DI 装配、`[UnitOfWork]` 自动事务、领域事件提交后分发、审计与软删除、模块生命周期、Dapper SQL/分页、三套方言、连接串负载策略、本地/Redis 锁、进程内事件总线、Quartz 调度、Redis 数据结构封装。

已知边界：没有 outbox（「已提交、未分发」会丢事件）、Dapper 通道不做审计也不过滤软删除、框架不带迁移（示例需手工建表）、Quartz 用内存 JobStore（重启不补跑）。逐条说明与优先级见 [design-notes.md](docs/design/design-notes.md)。

## 许可证

MIT 许可证。仓库目前尚未附带 `LICENSE` 文件，正式发布前应补上标准 MIT 文本。
