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
dotnet test Test/TestUnit
dotnet run --project samples/Webs/Web
dotnet run --project samples/Scheduler/Job
```

- 需要 .NET SDK 8.0；数据库示例用 MySQL 8，Redis 示例用 Redis 6+。
- 跑示例前必须先补 `samples/Webs/Web/appsettings.json` 的 `DBConnections` / `RedisDBConnections`，否则启动即抛「未设置连接字符串」（见 [docs/samples.md](docs/samples.md#21-先补配置)）。
- **不要声称「已通过测试」而没真正执行 `dotnet test`**。若环境无法执行命令（例如 Windows 沙箱报 `SetNamedSecurityInfoW failed (Win32 5): grantWrite(...)`），如实说明「未验证」，并给出手动验证命令。

## 3. 仓库结构

```
src/          框架类库，模块之间只允许依赖 Frame.Core
samples/      示例，只引用 src，不被任何项目引用
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

以下行为与「直觉」不符，改代码或写文档前务必先看 [docs/design-notes.md](docs/design-notes.md)：

1. **写操作延迟持久化**：EF Core 通道的 `Insert/Update/Delete` 只做变更追踪并返回 `0`，必须显式 `SaveChangesAsync`，而 `DatabaseContext` 未暴露它——只能经仓储的 `protected IDbContext DBContext` 调用。
2. **`[UnitOfWork]` 没有拦截器**：标注后不会自动开启/提交事务。
3. **领域事件没有分发器**：`Entity.DomainEvents` 只是容器。
4. **模块生命周期钩子不执行**：`IModuleInitializer` 注册后无人解析。
5. **审计接口不存在**：`IHasCreationTime` 等仅出现在注释里，不要在新代码/文档中引用。
6. **`RedisDbContext.Dispose` 会关闭全局 `ConnectionMultiplexer`**，第二次请求可能失效。
7. **直接注入 `IDbContext` 会抛异常**（父 Scope 连接串访问器未初始化），必须经 `DatabaseContext.GetRepository<T>()`。

修复这些问题时，先更新 `docs/design-notes.md` 的对应条目，再改代码与测试。

## 7. 改动检查清单

- [ ] `dotnet build` 通过（或明确说明未执行及原因）
- [ ] `dotnet test Test/TestUnit` 通过
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
