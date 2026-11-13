---
title: 测试
audience: 贡献者
status: stable
updated: 2026-11-13
sources:
  - Test/TestUnit
  - samples/TaskBoard/Tests
---

# 测试

仓库里有两个 xUnit 测试项目（TFM `net8.0`）：

| 项目 | 测什么 | 用例数 |
| --- | --- | --- |
| `Test/TestUnit` | 框架本身（`Frame.*`），纯逻辑，不碰数据库/Redis/HTTP | 99 |
| `samples/TaskBoard/Tests` | 示例项目集的领域层不变量 | 13 |

合计 112 个用例。

## 1. 运行

```bash
# 全部
dotnet test Frames.sln

# 只跑框架
dotnet test Test/TestUnit
dotnet test Test/TestUnit --filter "FullyQualifiedName~UnitOfWork"

# 只跑示例的领域测试
dotnet test samples/TaskBoard/Tests/Tests.csproj
```

在 .NET SDK 10.0.401 环境下实测：**112 个用例全部通过，0 警告 0 错误**（`dotnet build Frames.sln` 同样 0 警告 0 错误）。改动后请以实际执行结果为准——不要把「应该能过」写成「已通过」。

## 2. Test/TestUnit 清单

| 文件 | 覆盖对象 | 用例数 | 覆盖内容 |
| --- | --- | --- | --- |
| `EntityTest.cs` | `Entity<TPrimaryKey>` | 7 | 同 Id 相等、不同 Id 不等、同引用、与 `null` 比较、双方 transient、不同类型、非实体对象 |
| `EntityDomainEventsTests.cs` | `IHasDomainEvents` | 5 | 显式接口暴露的事件集合、添加/移除/清空、未添加时为 `null` 或空 |
| `PageResultTest.cs` | `PageResult<T>` | 2 | `Items`/`Count` 赋值与读取 |
| `PageInputTest.cs` | `PageInput` | 2 | 默认值 `Page = 1`、`Limit = 20` |
| `ConnectionStringCollectionTest.cs` | `ConnectionStringCollection` | 7 | 构造、`Add`、`AddRange`、`Random`、`RoundRobin` 30 次覆盖 3 个连接、空集合抛 `InvalidOperationException` |
| `RedisConnectionStringCollectionTest.cs` | `RedisConnectionStringCollection` | 8 | 同上（Redis 版），另含无参 `GetConnection()` 默认轮询 |
| `DbParametersTest.cs` | `DbParameters` | 27 | 构造（空/匿名对象）、`Add` 链式、`AddIf`、`AddOutput`、`AddReturn`、`Get` 未执行抛 `InvalidOperationException`、`TryGet`、`Remove`（大小写不敏感）、`Clear`、`Merge`、组合场景 |
| `EventBusInterfacesTest.cs` | `IEvent`/`IEventHandler<>`/`IEventBus` | 2 | 接口契约与泛型约束 |
| `LockEnumsTest.cs` | `LockType`/`LockState` | 2 | 枚举值 |
| `LockOptionsTest.cs` | `LockOptions` | 3 | 连接串集合、空集合、可变属性 |
| `ScheduledAttributeTest.cs` | `ScheduledAttribute` | 3 | 必填参数、Description 默认值、`null` 抛 `ArgumentNullException` |
| `JobDescriptorTest.cs` | `JobSchedule` | 2 | 属性赋值、`null` 参数抛异常 |
| `AuditPropertySetterTests.cs` | `AuditPropertySetter` | 7 | 创建/修改/软删除三类字段填充、只实现部分接口、`null` 参数抛异常、时间来自 `IClock` |
| `UnitOfWorkTests.cs` | `UnitOfWork`/`UnitOfWorkManager` | 8 | `Items` 在嵌套间共享、嵌套处理器提升到 Root、完成处理器按注册序、回滚处理器逆序、回滚后提交抛异常、`Dispose` 自动回滚、`CompleteAsync` 清空 `Current`、嵌套完成后外层回滚 |
| `UnitOfWorkProxyTests.cs` | `UnitOfWorkProxy` | 8 | 正常提交、`Task<int>` 返回值透传、异常回滚、同步方法立即提交、`null` 目标抛异常、非泛型工厂、接口方法特性、实现方法特性 |
| `ServiceApplicationConfigurationTests.cs` | `UseApplication` | 2 | 接口方法带 `[UnitOfWork]` 时注册为代理工厂、普通服务直接注册为 `Scoped` 实现类型 |
| `ModuleInitializerTests.cs` | `ModuleInitializer`/`SortModulesByDependency` | 4 | 初始化正序、关闭逆序、`null` 参数、依赖模块排在前面 |

## 3. samples/TaskBoard/Tests 清单

| 文件 | 覆盖对象 | 用例数 | 覆盖内容 |
| --- | --- | --- | --- |
| `ProjectTests.cs` | `Project` 聚合 | 7 | 创建时规范化字段并产生事件、空编码/空名称抛异常、改名事件、同名不发事件、归档状态与事件、重复归档抛异常、归档后改名抛异常 |
| `TaskItemTests.cs` | `TaskItem` 聚合 | 5 | 创建默认 `Todo` 且 `DueDate` 转 UTC、`projectId = 0` 抛异常、完成置 `Done` 并产生事件、重复完成抛异常、完成后 `Start` 抛异常 |

它只引用 `Domain`，不启动宿主、不连数据库，所以能在 CI 里毫秒级跑完。

## 4. 覆盖到与没覆盖到

**已覆盖（纯逻辑）**：

- 实体相等性语义与领域事件容器（框架最容易回归的地方）；
- UOW 的 `Items` 共享、处理器顺序、嵌套提升、回滚与 `AsyncLocal` 清理；
- `DispatchProxy` 拦截器的提交/回滚/返回值改写（含 `Task<T>`）；
- 审计字段填充规则；
- 模块生命周期顺序与依赖排序；
- 连接串选择策略、`DbParameters` 装配、特性/选项参数校验；
- 示例的项目/任务聚合不变量（归档后不能改名、任务不能重复完成等）。

**未覆盖（也是风险最高的部分）**：

| 缺口 | 风险 |
| --- | --- |
| `AddFrameService` 完整装配 | BFS 程序集发现、各服务生命周期、代理注册（排序函数本身已单测） |
| `DatabaseContext` + 仓储解析 | `GetRepository` 的 Scope/连接串/`DbContextScope` 协同、`AttachToUnitOfWork` 去重 |
| `DbContext` 的审计与变更收集 | `ApplyAuditing` 的状态迁移、软删除转 `Modified`、`GetChangedEntities`/`GetTrackedEntities` |
| 领域事件端到端 | 事务提交 → 分发 → 清空，依赖真实 `DatabaseContext` 与 dispatcher；「显式 `SaveChanges` 后事件仍能分发」目前只有示例可验证 |
| `DbContext` 的 SQL 生成 | 三种方言的分页 SQL、参数化、`GetTableName` 缓存 |
| `LocalEventBus` | 投递、重试退避、Handler 扫描、多态缺失 |
| `QuartzScheduler` | cron 转换（5 段→6 段、`*`→`?`）、JobKey 判重 |
| `LocalLock`/`RedisLock` | 获取失败返回 `IsAcquired=false`、并发互斥 |

后几项需要 Testcontainers（MySQL、Redis）或 `WebApplicationFactory` 级别的集成测试。当前项目里没有任何 `Microsoft.AspNetCore.Mvc.Testing` 或 Testcontainers 依赖。

## 5. 测试项目的依赖

`Test/TestUnit/TestUnit.csproj` 以 `ProjectReference` 方式引用 `src` 下的框架项目（`Frame.Core`、`Frame.Databases`、`Frame.Locks`、`Frame.EventBus`、`Frame.Scheduler`、`Frame.Redis`），没有引用 `samples`。

`samples/TaskBoard/Tests/Tests.csproj` 只引用 `samples/TaskBoard/Domain`。

`Frame.Core` 通过 `src/Frame.Core/AssemblyInfo.cs` 的 `[assembly: InternalsVisibleTo("TestUnit")]` 暴露 `SortModulesByDependency` 等内部成员。

## 6. 给维护者的建议

1. **补装配测试**：用 `ServiceCollection` + `AddFrameService` 断言关键注册（`IUnitOfWorkManager` 为 Scoped、`ILockFactory` 为 Singleton、带 `[UnitOfWork]` 的服务解析为代理、`IDomainEventDispatcher` 是否注册）。
2. **补数据库集成测试**：用 SQLite 内存库验证 `ApplyAuditing`、`GetChangedEntities`/`GetTrackedEntities`，以及「UOW 提交后数据可见、回滚后不可见」「显式 `SaveChanges` 后事件仍分发」。
3. **补 SQL 生成测试**：三种方言的 `BuildPagedSql` 输出快照（不连库即可测）。
4. **集成测试用 Testcontainers**：MySQL 容器验证落库与 `QueryPaged`；Redis 容器验证锁与 `RedisContext` 生命周期（断言第二个请求仍能访问 Redis）。
5. **示例也要有集成测试**：`samples/Webs` 与 `samples/TaskBoard/Web` 目前只能手工 curl 验证，接入 `WebApplicationFactory` 后可以把 `docs/samples.md` 里的请求固化成测试。

相关文档：[设计说明与已知差距](../design/design-notes.md) · [工作单元与自动事务](../concepts/unit-of-work.md) · [示例工程](../guide/samples.md) · [贡献指南](../../CONTRIBUTING.md)
