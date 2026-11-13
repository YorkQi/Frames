# 更新日志

本项目尚未发布 NuGet 包，版本号从 `0.1.0` 起按「框架能力」递增。格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [Unreleased]

### 新增

- `[UnitOfWork]` 自动事务：`UnitOfWorkProxy` 基于 `DispatchProxy` 为应用服务生成代理，方法进入时 `Begin`、正常返回/`Task` 完成后 `CompleteAsync`、异常时 `RollbackAsync`；方法级特性优先于类级，支持 `IsAutoRollback` 与 `Timeout`。
- 工作单元扩展：`IUnitOfWork.Items`（嵌套共享同一字典）、`RegisterCompletionHandler` / `RegisterRollbackHandler`（嵌套注册自动提升到最外层，完成按注册序、回滚逆序）。
- 领域事件分发：`Entity` 实现 `IHasDomainEvents`，新增 `IDomainEventDispatcher`；UOW 提交成功后由 `DatabaseContext` 收集被追踪实体上挂起的 `DomainEvents` 并逐个分发，然后清空。`Frame.EventBus` 提供 `EventBusDomainEventDispatcher`（仅投递 `IEvent`）。
- 审计字段：`Frame.Core/Auditing` 提供 `IHasCreationTime`、`ICreationAuditedObject`、`IHasModificationTime`、`IModificationAuditedObject`、`ISoftDelete`、`IHasDeletionTime`、`IDeletionAuditedObject`、`IAuditedObject`、`IFullAuditedObject`，以及 `IClock` / `SystemClock`、`ICurrentUser` / `NullCurrentUser`、`IAuditPropertySetter` / `AuditPropertySetter`。EF Core 通道在 `SaveChanges` 前自动填充创建/修改/软删除字段。
- 模块生命周期：`IModuleInitializer` / `ModuleInitializer` 公开，新增 `ModuleInitializationHostedService`，在宿主启动时按依赖序调用 `OnApplicationInitializationAsync`、停止时逆序调用 `OnApplicationShutdownAsync`。
- `DatabaseContext` 自动落库：`GetRepository` 时把 `IDbContext` 挂到当前 UOW，提交时依次收集变更与事件、开启事务、`SaveChanges`、提交、分发事件、清空事件；回滚时调用 `IDbContext.Rollback()`。
- `IDBContext.GetChangedEntities()`：返回 `ChangeTracker` 中 Added/Modified/Deleted 的实体，用于判断是否需要落库。
- `IDBContext.GetTrackedEntities()`：返回 `ChangeTracker` 中全部被追踪实体（含 `Unchanged`），用于收集领域事件——应用服务为取自增主键而显式 `SaveChanges` 后，事件仍能在 UOW 完成时分发。
- 单元测试：新增 `AuditPropertySetterTests`、`EntityDomainEventsTests`、`UnitOfWorkTests`、`UnitOfWorkProxyTests`、`ModuleInitializerTests`、`ServiceApplicationConfigurationTests`，框架共 **99 个用例通过**。
- `samples/TaskBoard` 小型项目集：`Domain / Application / Infrastructure / Web / Worker / Tests` 六个项目，演示多宿主共用组合根（`AddTaskBoard`）、接口级 `[UnitOfWork]`、软删除、领域事件与属性式定时任务；新增 13 个领域测试用例。
- `samples/Webs` 重写为 REST API 最小示例：`User` 聚合（审计 + 软删除 + 领域事件）、`UserService`（接口级 `[UnitOfWork]`、Redis 缓存、业务异常）、`UsersController`、`ExceptionFilter`、`HttpContextCurrentUser`、带占位值的 `appsettings.json`；删除旧 `HomeController` 验收脚本与 Razor 视图。
- `[UnitOfWork]` 现在也识别标在**接口或接口方法**上的特性（`RequiresUnitOfWork` 同时检查实现类与接口，拦截器也会读取实现方法上的特性）。

### 变更

- **`DeleteBatch` / `DeleteBatchAsync` 改为延迟持久化**：不再直接执行 `DELETE FROM ... WHERE Id IN @ids`，而是逐个 `Find` 后 `Remove` 并返回 `0`，与单条 `Delete` 一致，由 UOW 提交时统一落库（软删除也因此生效）。依赖「批量删除立即生效」的代码需要显式提交。
- `Frame.Core` 增加对 `Microsoft.Extensions.Hosting.Abstractions` 8.0.0 的依赖（`IHostedService`）。
- `DbContext` 构造函数新增可选参数 `IAuditPropertySetter? auditPropertySetter = null`。
- `UnitOfWorkManager.Begin` 在最外层继承 `DefaultOnComplete` / `DefaultOnRollback`；`AsyncLocal` 改为持有可变 holder，修复「`CompleteAsync` 后 `Current` 不清空」。
- **`RedisDbContext.Dispose` 不再关闭全局 `ConnectionMultiplexer`**：改为空实现，连接生命周期交给 DI Singleton，Scoped `RedisContext` 可以安全按请求释放。
- 领域事件的来源从「变更实体」扩大为「所有被追踪实体」，避免显式 `SaveChanges` 后事件丢失。
- 三个示例的 `appsettings.json` 提供占位连接串；运行前需替换为真实地址并手工建表。

### 文档

- 重写根 `README.md`：模块表、依赖图、快速开始、文档导航、能力状态表。
- 新增 `docs/` 文档集：索引、快速开始、架构、核心、工作单元、审计、领域事件、模块结构、数据库、锁、事件总线、调度、Redis、示例、设计说明、测试。
- 新增 `AGENTS.md`：面向 AI 编码助手与新人的环境、命令、编码约定、实现现状与改动检查清单。
- 新增 `CONTRIBUTING.md` 与 `CHANGELOG.md`。
- 全站文档统一使用中文（保留代码、类型名、异常消息的原文）。
- 重写 `docs/samples.md`：三个示例的定位、Webs 分层走读与 curl 流程、TaskBoard 项目集方案（分层、`AddTaskBoard` 组合根、端点表、Worker、扩展清单）、建表 SQL 与已知限制。
- 同步 `docs/design-notes.md`（Redis Dispose 已修、示例配置与建表、事件来源）、`docs/redis.md`（4.1 改为已修复）、`docs/testing.md`（112 用例与 TaskBoard 测试）、`docs/core.md`、`docs/unit-of-work.md`、`docs/domain-events.md`、`docs/design-notes.md` 的事件收集描述。
- **文档重新规划为四层**：`guide/`（怎么做）、`concepts/`（是什么/为什么）、`design/`（当时为什么 + ADR）、`ops/`（怎么验）；原 `docs/*.md` 平铺结构迁移到对应目录，相对链接与锚点全部重算。
- 新增 `docs/manifest.json`：页面清单与事实数字（程序集数、用例数）的唯一来源；根 README、根 AGENTS、`ops/testing.md` 只保留声明位。
- 新增 `scripts/check-docs.ps1`（D1–D10：manifest 覆盖、链接、锚点、front matter、程序集/TFM、用例数、裸数字、表格形状、ADR 索引、锚点唯一）与 `docs/ops/doc-consistency.md`（规则与失败处理）。
- 新增 `.githooks/pre-commit`（提交前跑 `check-docs.ps1 -Quiet`）与 `.github/workflows/docs.yml`（CI 跑 `-VerifyTests`）。
- 新增 `docs/design/adr/0001–0004`：工作单元代理、事件提交后分发、审计填充位置、示例单一组合根。
- 新增 `docs/guide/recipes.md`：加聚合/端点/落库/事件/软删除/锁/定时任务/Redis 缓存/多宿主/补测试共 10 条配方。
- 根 `README.md`、`AGENTS.md`、`docs/README.md` 改为地图式入口（按任务索引、渐进披露）；新增 `src/*/AGENTS.md`、`samples/AGENTS.md`、`Test/AGENTS.md`、`.claude/memory/AGENTS.md`。
- 修正原 README 中与代码不符的描述，并随四项能力落地更新：
  - `[UnitOfWork]` 已由 `DispatchProxy` 拦截并自动开启/提交事务；
  - 领域事件已由 `DatabaseContext` 在提交后分发；
  - 审计与软删除契约已补齐并在 EF Core 通道自动填充；
  - 模块生命周期钩子已通过 `ModuleInitializationHostedService` 接线；
  - EF Core 写操作仍是延迟持久化，但会在 UOW 完成时自动提交；
  - 修正「仓储由 `UseApplication()` 注册」为「由 `UseDatabaseContext()` 内部的 `UseRepositories()` 注册」；
  - 补充 `Frame.Redis` 的 `ConnectionMultiplexer` 生命周期；原「`RedisDbContext.Dispose` 关闭全局连接」的问题已修复。

## [0.1.0] - 未发布

### 新增（初始版本）

- `Frame.Core`：实体基类、仓储契约、工作单元、应用服务基类、`[ServiceModule]`/`[DependsOn]` 模块系统、`AddFrameService` 程序集发现与装配。
- `Frame.Databases`：Dapper + EF Core 双通道 `IDbContext`、`DatabaseContext` 作用域、三种方言（MySQL / SQL Server / PostgreSQL）的分页 SQL、`DbParameters`、`PageInput`/`PageResult`。
- `Frame.Locks`：`ILock`/`ILockFactory`、进程内 `Monitor` 锁、基于 RedLock.net 的 Redis 分布式锁。
- `Frame.EventBus`：进程内事件总线、`IHostedService` 消费循环、指数退避重试。
- `Frame.Scheduler`：Quartz 包装、`[Scheduled]` 特性扫描与配置列表两种注册方式、5 段 cron 自动补秒。
- `Frame.Redis`：`RedisContext` + `IRedisDbContext`，覆盖 Key/String/List/Set/Hash/SortedSet/PubSub 常用命令。
- `samples/Webs`：ASP.NET Core 五层 DDD 示例；`samples/Scheduler/Job`：定时任务示例。
- `Test/TestUnit`：实体、分页、连接串、参数、特性等纯逻辑单元测试。
