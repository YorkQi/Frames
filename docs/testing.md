# 测试

`Test/TestUnit` 是 xUnit 单元测试项目（TFM `net8.0`），只测**纯逻辑**：没有数据库、没有 Redis、没有 HTTP。

## 1. 运行

```bash
dotnet test Test/TestUnit
# 或指定过滤
dotnet test Test/TestUnit --filter "FullyQualifiedName~EntityTest"
```

当前环境（本次整理文档的会话）里 `pwsh` 被沙箱 ACL 挡住，无法实际执行 `dotnet test`；以下结论来自逐文件阅读测试源码。

## 2. 测试清单

| 文件 | 覆盖对象 | 用例数 | 覆盖内容 |
| --- | --- | --- | --- |
| `EntityTest.cs` | `Entity<TPrimaryKey>` | 7 | 同 Id 相等、不同 Id 不等、同引用、与 `null` 比较、双方 transient、不同类型、非实体对象 |
| `PageResultTest.cs` | `PageResult<T>` | 2 | `Items`/`Count` 赋值与读取 |
| `PageInputTest.cs` | `PageInput` | — | 默认值 `Page = 1`、`Limit = 20` |
| `ConnectionStringCollectionTest.cs` | `ConnectionStringCollection` | 7 | 构造、`Add`、`AddRange`、`Random`、`RoundRobin` 30 次覆盖 3 个连接、空集合抛 `InvalidOperationException` |
| `RedisConnectionStringCollectionTest.cs` | `RedisConnectionStringCollection` | — | 与上者对称的 Redis 版 |
| `DbParametersTest.cs` | `DbParameters` | 27 | 构造（空/匿名对象）、`Add` 链式、`AddIf`、`AddOutput`、`AddReturn`、`Get` 未执行抛 `InvalidOperationException`、`TryGet`、`Remove`（大小写不敏感）、`Clear`、`Merge`、组合场景 |
| `EventBusInterfacesTest.cs` | `IEvent`/`IEventHandler<>`/`IEventBus` | — | 接口契约与泛型约束 |
| `LockEnumsTest.cs` | `LockType`/`LockState` | — | 枚举值 |
| `LockOptionsTest.cs` | `LockOptions` | — | 连接串集合 |
| `ScheduledAttributeTest.cs` | `ScheduledAttribute` | 3 | 必填参数、Description 默认值、`null` 抛 `ArgumentNullException` |
| `JobScheduleTest.cs` | `JobSchedule` | 2 | 属性赋值、`null` 参数抛异常 |

## 3. 覆盖到与没覆盖到

**已覆盖（纯逻辑）**：

- 实体相等性语义（框架最容易回归的地方）；
- 连接串选择策略（含轮询分布与空集合异常）；
- `DbParameters` 的参数装配与「未执行读取输出参数」的守卫；
- 特性/选项对象的参数校验。

**未覆盖（也是风险最高的部分）**：

| 缺口 | 风险 |
| --- | --- |
| `AddFrameService` 装配 | BFS 程序集发现、模块拓扑排序、DI 生命周期——无任何测试 |
| `UnitOfWork` 嵌套/提交/回滚 | 异步 `AsyncLocal` 传播、嵌套语义、Dispose 自动回滚 |
| `DatabaseContext` + 仓储解析 | `GetRepository` 的 Scope/连接串/`DbContextScope` 协同 |
| `DbContext` 的 SQL 生成 | 三种方言的分页 SQL、参数化、`GetTableName` 缓存 |
| `LocalEventBus` | 投递、重试退避、Handler 扫描、多态缺失 |
| `QuartzScheduler` | cron 转换（5 段→6 段、`*`→`?`）、JobKey 判重 |
| `LocalLock`/`RedisLock` | 获取失败返回 `IsAcquired=false`、并发互斥 |

这些都需要 `WebApplicationFactory` / Testcontainers（MySQL、Redis）级别的集成测试。当前项目里没有任何 `Microsoft.AspNetCore.Mvc.Testing` 或 Testcontainers 依赖。

## 4. 测试项目的依赖

`Test/TestUnit.csproj` 以 `ProjectReference` 方式引用 `src` 下的框架项目（`Frame.Core`、`Frame.Databases`、`Frame.Locks`、`Frame.EventBus`、`Frame.Scheduler`、`Frame.Redis`），没有引用 `samples`。因此测试只验证框架本身，不验证示例。

## 5. 给维护者的建议

1. **补装配测试**：用 `ServiceCollection` + `AddFrameService` 断言关键注册（`IUnitOfWorkManager` 为 Scoped、`ILockFactory` 为 Singleton、Handler 为 Scoped）。
2. **补 UOW 测试**：嵌套 `Begin` 后只提交最外层、异常时冒泡回滚、`Dispose` 自动回滚。
3. **补 SQL 生成测试**：三种方言的 `BuildPagedSql` 输出快照（不连库即可测）。
4. **集成测试用 Testcontainers**：MySQL 容器验证 `SaveChanges`/`QueryPaged`；Redis 容器验证锁与 `RedisContext` 生命周期（顺便能暴露 design-notes 第 7 条的缺陷）。
5. **给已修的问题加回归用例**：例如 `RedisDbContext.Dispose` 修复后，断言第二次解析 `RedisContext` 仍可用。
