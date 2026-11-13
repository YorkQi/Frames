# AGENTS.md

本文件是**仓库地图**，给 AI 编码助手与新加入的开发者。它只做三件事：告诉你去哪读、哪些命令能跑、哪些坑不能踩。细节一律在链接里——不要指望这一页装下全部。

人类贡献流程见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 1. 这是什么

Frames 是一个 .NET 分层/模块化应用框架，按 DDD 四层组织，入口是 `AddFrameService(options => ...)`。六个程序集：

| 程序集 | TFM | 一句话 |
| --- | --- | --- |
| `src/Frame.Core` | `netstandard2.1` | 实体、仓储契约、UOW、审计契约、模块系统、DI 引导 |
| `src/Frame.Databases` | `net8.0` | EF Core + Dapper 双通道 `IDbContext`、方言、分页 |
| `src/Frame.Locks` | `netstandard2.1` | 本地锁 / RedLock |
| `src/Frame.EventBus` | `netstandard2.1` | 进程内事件总线 |
| `src/Frame.Scheduler` | `netstandard2.1` | Quartz 包装 + `[Scheduled]` |
| `src/Frame.Redis` | `netstandard2.1` | StackExchange.Redis 访问层 |

`src/` 各程序集另有自己的 `AGENTS.md`，说明目录职责与改动边界。

## 2. 命令

```bash
dotnet restore
dotnet build Frames.sln
dotnet test Frames.sln                     # 112 个用例（框架 99 + 示例 13）

# 文档一致性（改过 *.md 或 manifest 就应跑）
powershell -NoProfile -File scripts/check-docs.ps1
powershell -NoProfile -File scripts/check-docs.ps1 -VerifyTests   # 顺带执行测试并核对数字

# 示例（先改 appsettings.json 里的占位连接串，并手工建表）
dotnet run --project samples/Webs/Web
dotnet run --project samples/TaskBoard/Web
dotnet run --project samples/TaskBoard/Worker
dotnet run --project samples/Scheduler/Job
```

首次 clone 后启用提交前检查：

```bash
git config core.hooksPath .githooks
```

**不要声称「已通过测试」而没真正执行 `dotnet test`。** 环境跑不了命令时如实说「未验证」，并给出手动命令（Windows 沙箱可能报 `SetNamedSecurityInfoW failed (Win32 5): grantWrite(...)`）。

## 3. 去哪读

| 你要做的事 | 读这里 |
| --- | --- |
| 跑起来 / 接进项目 | [docs/guide/getting-started.md](docs/guide/getting-started.md) |
| 加一个具体功能 | [docs/guide/recipes.md](docs/guide/recipes.md) |
| 理解装配、生命周期、模块 | [docs/concepts/architecture.md](docs/concepts/architecture.md)、[modules.md](docs/concepts/modules.md) |
| 改 UOW / 事件 / 审计 / 数据库 | `docs/concepts/` 对应页 |
| 判断某设计是不是有意为之 | [docs/design/design-notes.md](docs/design/design-notes.md)、[ADR](docs/design/adr/README.md) |
| 写/改文档 | [docs/AGENTS.md](docs/AGENTS.md) |
| 弄清测试覆盖 | [docs/ops/testing.md](docs/ops/testing.md) |

文档目录分工：`guide/` 怎么做、`concepts/` 是什么/为什么、`design/` 当时为什么这么选、`ops/` 怎么验证。不要把四类混进一页。

## 4. 硬约束

- **中文文档**：面向人的文字全部中文；代码、类型名、异常消息、命令保持原文。
- **以代码为准**：写「支持 X」前先看源码；不确定就写「⚠️ 未接线」并链到 design-notes。
- **签名照抄**：类型名、方法名、参数顺序、返回类型（例如 `GetAsync` 返回 `Task<TEntity>`）必须与源码一致。
- **不改 TFM**：`netstandard2.1` 里不能用 `net8.0` 独有 API；新增依赖前先确认 BCL 够不够。
- **引用方向**：`src/` 模块之间只依赖 `Frame.Core`；`samples` / `Test` 不得被 `src` 引用。
- **文档事实只改一处**：用例数、程序集数写在 `docs/manifest.json`，再由 `check-docs.ps1` 校验各处声明，不要手改散落数字。
- **API 变更同步三处**：对应 `docs/` 页面、根 `README.md` 能力表、`CHANGELOG.md` 的 `[Unreleased]`。

## 5. 必须知道的实现现状

四项能力（自动事务、领域事件、审计、模块生命周期）**已实现但都有边界**。改代码前先读 [design-notes](docs/design/design-notes.md)：

1. **自动落库的前提是「UOW + `GetRepository`」**：写操作延迟并返回 `0`，落库发生在 UOW 完成处理器；只解析 `DatabaseContext` 而没调 `GetRepository<T>()`、或不在 UOW 内，都不会提交。
2. **`[UnitOfWork]` 靠 `DispatchProxy`**：只能代理接口方法；类内 `new` 的对象、`Dispose`、非接口成员不受影响。拦截器必须 `public` 且非 `sealed`。
3. **领域事件只在 UOW 提交成功后可靠**：事件取自 `ChangeTracker` 中全部被追踪实体（含已 `SaveChanges` 的 `Unchanged`），但 Dapper 写入与无 UOW 的保存不分发；崩溃丢事件（无 outbox）；`IDomainEventDispatcher` 仅 `UseEventBus` 注册，缺失时事件被静默清空。
4. **模块生命周期走 `ModuleInitializationHostedService`**：宿主必须真正运行 `IHostedService`；初始化按依赖正序、关闭逆序；排序是 DFS 后序、无环检测。
5. **审计与软删除只在 EF 通道生效**：Dapper 手写 SQL 不填审计，软删除后也没有全局查询过滤。
6. **`RedisDbContext.Dispose` 是空实现**，连接由 DI Singleton 管理；但停止时无显式优雅关闭，且注册阶段同步 `Connect`，Redis 不可用会拖慢启动。
7. **直接注入 `IDbContext` 会抛异常**（父 Scope 连接串访问器未初始化），必须经 `DatabaseContext.GetRepository<T>()`——这也是示例里 Application 必须引用 Infrastructure 的原因。

## 6. 改完自检

- [ ] `dotnet build Frames.sln` 通过（或说明未执行原因）
- [ ] `dotnet test Frames.sln` 通过
- [ ] 改过文档 → `scripts/check-docs.ps1` 通过
- [ ] 新增/修改的公共 API 有 XML 注释
- [ ] 相关 `docs/` 页面、`manifest.json` 与 `CHANGELOG.md` 已同步
- [ ] 示例代码与源码签名一致；未引入不必要依赖、未改 TFM

## 7. 提交

格式 `模块: 动作`：

```
Databases: 为 DatabaseContext 增加 SaveChangesAsync
Docs: 按 guide/concepts/design/ops 重组文档并接入一致性检查
```

一个提交只做一件事；行为变化写清动机、影响面与验证方式。
