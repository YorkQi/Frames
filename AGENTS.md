# AGENTS.md

本文件面向在本仓库中工作的 AI 编码助手（以及新加入的开发者），说明环境、命令、约定与必须知道的坑。人类贡献者请同时阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 1. 项目是什么

Frames 是一个 .NET 的分层/模块化应用框架，按 DDD 四层组织，核心入口是 `AddFrameService(options => ...)`。

| 程序集 | TFM | 职责 |
| --- | --- | --- |
| `src/Frame.Core` | `netstandard2.1` | 实体、仓储契约、UOW、模块系统、DI 引导 |
| `src/Frame.Databases` | `net8.0` | EF Core + Dapper 双通道 `IDbContext`、方言、分页、`DbParameters` |
| `src/Frame.Locks` | `netstandard2.1` | 本地 `Monitor` 锁 / RedLock 分布式锁 |
| `src/Frame.EventBus` | `netstandard2.1` | 进程内事件总线（Channel + 重试） |
| `src/Frame.Scheduler` | `netstandard2.1` | Quartz 包装 + `[Scheduled]` |
| `src/Frame.Redis` | `netstandard2.1` | StackExchange.Redis 访问层 |

`samples/*` 与 `Test/TestUnit` 为 `net8.0`。

## 2. 环境与命令

```bash
dotnet restore
dotnet build
dotnet test Frames.sln
dotnet run --project samples/Webs/Web
dotnet run --project samples/Scheduler/Job
dotnet run --project samples/TaskBoard/Web
dotnet run --project samples/TaskBoard/Worker
```

- 需要 .NET SDK 8.0；数据库示例用 MySQL 8，Redis 示例用 Redis 6+。
- 跑示例前必须补 `samples/Webs/Web/appsettings.json` 的 `DBConnections` / `RedisDBConnections`、以及 `samples/TaskBoard/{Web,Worker}/appsettings.json` 的 `TaskBoard:Connections:Database`；连接串是占位值，还要手工建表（见 [docs/samples.md](docs/samples.md#21-先补配置) 与 [建表 SQL](docs/samples.md#5-数据库与表结构)）。
- **不要声称「已通过测试」而没真正执行 `dotnet test`**。若环境无法执行命令（例如 Windows 沙箱报 `SetNamedSecurityInfoW failed (Win32 5): grantWrite(...)`），如实说明「未验证」，并给出手动验证命令。

## 3. 仓库结构

```
src/          框架类库，模块之间只允许依赖 Frame.Core
samples/      示例，只引用 src，不被任何项目引用；Webs 是最小 API，TaskBoard 是多宿主项目集
Test/         测试，TestUnit 引用 src
docs/         面向人的文档，与代码同步维护
.claude/memory/  早期 AI 速记，内容可能过时，冲突时以 docs/ 与源码为准
```

## 4. C# 编码约定

- file-scoped namespace；私有字段 `_camelCase`；公共 API 必须有 XML 注释。
- 异步方法以 `Async` 结尾，并接受 `CancellationToken cancellationToken = default`。
- 新增依赖前先确认能否用 BCL 完成；**不要**随意升级 TFM——`netstandard2.1` 项目里不能用 `net8.0` 独有 API。
- 异常消息用中文，并与现有风格一致（例如 `"连接字符串未设置。请先设置 ConnectionStringAccessor。"`）；参数校验用 `ArgumentNullException` / `ArgumentOutOfRangeException`。
- 扩展方法统一挂在 `ServiceConfigurationContext` 上，命名为 `UseXxx`，只注册自己的服务，不假设宿主环境。

## 5. 文档约定（重要）

- **全部用中文**写（代码、类型名、异常消息除外）。新增文档默认放 `docs/`，并在 [docs/README.md](docs/README.md) 与根 README 的导航表登记。
- **以代码为准**：写「支持 X」前必须在源码里确认。不确定就写「当前未接线」，并链到 [docs/design-notes.md](docs/design-notes.md)。
- 代码示例必须能对照源码编译：类型名、方法名、参数顺序、返回类型（例如 `GetAsync` 返回 `Task<TEntity>` 而非 `Task<TEntity?>`）都要准确。
- 修改公共 API 时同步更新：对应 `docs/*.md`、根 `README.md` 的能力表、`CHANGELOG.md` 的 `[Unreleased]`。
- 跨文档链接用相对路径，锚点必须与目标标题一致（中文标题的锚点形如 `#4-延迟持久化`）。

## 6. 必须知道的实现现状

四项能力（`[UnitOfWork]` 自动事务、领域事件分发、审计字段、模块生命周期钩子）**已实现**，但都有明确边界。改代码或写文档前务必先看 [docs/design-notes.md](docs/design-notes.md)：

1. **自动落库的前提是「UOW + `GetRepository`」**：写操作仍然延迟（返回 `0`），落库发生在 UOW 完成处理器里。只解析了 `DatabaseContext` 但没调 `GetRepository<T>()`，或根本不在 UOW 内，都不会自动提交。
2. **`[UnitOfWork]` 靠 `DispatchProxy` 实现**：只能代理接口、只拦截接口方法；类里 `new` 出来的对象、`Dispose`、非接口成员不受影响。`UnitOfWorkInterceptor` 必须 `public` 且非 `sealed`。
3. **领域事件只在 UOW 提交成功且用打过 `[UnitOfWork]` 的服务时可靠**：事件来自 `ChangeTracker` 中被追踪的全部实体（含已显式 `SaveChanges` 的 `Unchanged` 实体），但 Dapper 写入与「没走 UOW」的保存不会分发；崩溃会丢事件（没有 outbox）；`IDomainEventDispatcher` 只有 `UseEventBus` 才注册，未注册时事件被静默清空。
4. **模块生命周期走 `ModuleInitializationHostedService`**：需要宿主真正执行 `IHostedService`；模块初始化按依赖排序执行，关闭逆序；`SortModulesByDependency` 是 DFS 后序、无环检测。
5. **审计与软删除只在 EF Core 通道生效**：Dapper 手写 SQL 不填审计字段，软删除后也没有全局查询过滤。
6. **`RedisDbContext.Dispose` 已改为空实现**，连接由 DI Singleton 管理；但应用停止时没有显式优雅关闭，且 `UseRedisDatabase` 在注册阶段同步 `Connect`，Redis 不可用会拖慢/中断启动。
7. **直接注入 `IDbContext` 会抛异常**（父 Scope 连接串访问器未初始化），必须经 `DatabaseContext.GetRepository<T>()`；这也是示例里 Application 必须引用 Infrastructure 的原因。

修复这些问题时，先更新 `docs/design-notes.md` 的对应条目，再改代码与测试。

## 7. 改动检查清单

- [ ] `dotnet build` 通过（或明确说明未执行及原因）
- [ ] `dotnet test Frames.sln` 通过（框架 99 + 示例 13）
- [ ] 新增/修改的公共 API 有 XML 注释
- [ ] 相关 `docs/` 页面与 `CHANGELOG.md` 已同步
- [ ] 没有引入不必要的依赖，没有改动 TFM
- [ ] 示例代码与源码签名一致
- [ ] 没有把 `samples` / `Test` 反向引用进 `src`

## 8. 提交信息

格式 `模块: 动作`，例如：

```
Databases: 为 DatabaseContext 增加 SaveChangesAsync
Docs: 补充 Redis 连接生命周期说明
```

一个提交只做一件事；行为变化必须在描述里写清动机、影响面与验证方式。
