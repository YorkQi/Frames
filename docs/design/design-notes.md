---
title: 设计说明与已知落差
audience: 贡献者
status: evolving
updated: 2026-11-13
sources:
  - src/Frame.Core/ServiceDependencyInjection.cs
  - src/Frame.Core/Uow
  - src/Frame.Databases
  - src/Frame.Redis/RedisDbContexts/RedisDbContext.cs
---

# 设计说明与已知落差

这份文档记录「框架想做什么」「代码实际做了什么」以及两者之间的差距。它不是 bug 列表，而是维护者的决策依据：哪些是刻意的简化，哪些是遗漏，哪些是明确的实现缺陷。

> 2026-11 的四项能力（`[UnitOfWork]` 自动事务、领域事件分发、审计字段、模块生命周期钩子）已实现。本文把这些条目标为「已实现」，并保留当时的动机与剩余限制。

## 0. 设计目标 vs 现状

| 目标 | 状态 | 说明 |
| --- | --- | --- |
| 模块化装配 | ✅ | `AddFrameService` + `IFrameModule` + `[DependsOn]` |
| 统一工作单元 | ✅ | `[UnitOfWork]` 经 `DispatchProxy` 自动开启/提交/回滚 |
| 领域事件随提交分发 | ✅ | UOW 提交成功后由 `IDomainEventDispatcher` 分发，无 outbox |
| 审计字段自动填充 | ✅ | 仅 EF Core 通道；Dapper 手写 SQL 不参与 |
| Dapper + EF 双通道 | ✅ | 同一个 `IDbContext` 暴露两套 API |
| 多方言分页 | ✅ | MySQL / SQL Server / PostgreSQL |
| 分布式锁 | ✅ | 本地 `Monitor` + RedLock |
| 进程内事件总线 | ✅ | Channel + 重试，无持久化 |
| 定时任务 | ✅ | Quartz 内存 JobStore |
| Redis 访问层 | ✅ | 连接由 DI Singleton 管理，`RedisDbContext.Dispose` 不再关闭全局连接（见第 7 节） |

## 1. `[UnitOfWork]` 自动事务

**动机**：业务方法不该手写 `Begin` / `Commit` / `Rollback`，也不该每个方法都记得 `SaveChanges`。

**实现**：`ServiceApplicationConfiguration.UseApplication` 发现类级或方法级 `[UnitOfWork]` 时，注册工厂用 `UnitOfWorkProxy.Create` 生成 `DispatchProxy` 代理；拦截器在调用前后管理 UOW，并把 `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>` / 同步返回值统一包装成「完成后提交、异常回滚」。

**为什么用 `DispatchProxy`**：`Frame.Core` 是 `netstandard2.1`，不想引入 Castle DynamicProxy 这类第三方依赖；`DispatchProxy` 是 BCL 自带，代价是只能代理接口。

**残留限制**：

- 只支持接口代理，`GetRequiredService<OrderService>()` 拿到的是无事务的原始实例；
- 一个类实现多个 `IApplicationService` 派生接口时只注册第一个（`GetInterfaces().FirstOrDefault`）；
- 同步方法的提交阶段异常不会触发回滚（回滚只在目标方法抛异常时发生）；
- 类级特性会给只读方法也开 UOW。

**当时的备选**：手写 `using var uow = manager.Begin()`（仍然支持，见 [core.md](../concepts/core.md#34-正确用法)）、Castle DynamicProxy（被依赖约束否决）。

## 2. 领域事件分发

**动机**：`Entity.DomainEvents` 原本只是容器，`AddDomainEvent` 是死代码。

**实现**：`Entity` 实现 `IHasDomainEvents`；`DatabaseContext.AttachToUnitOfWork` 注册完成处理器，在 `SaveChanges` 前取走所有被追踪实体上挂起的 `DomainEvents`，提交成功后逐个 `IDomainEventDispatcher.DispatchAsync`，最后 `ClearDomainEvents`。`Frame.EventBus` 提供 `EventBusDomainEventDispatcher`，把实现了 `IEvent` 的事件 `Push` 进总线。

**为什么在提交后分发**：回滚时不应外发事件。代价是没有 outbox——「已提交、未分发」之间崩溃会丢事件。

**残留限制**：

- 只覆盖 EF Core 通道：Dapper 写入没有变更追踪，不会触发事件分发；
- 必须经过 UOW 且用过 `DatabaseContext.GetRepository`，否则没有完成处理器；
- 事件对象不实现 `IEvent` 时被 `LogWarning` 丢弃（想自定义就替换 `IDomainEventDispatcher`）；
- 分发异常会让业务方法失败，但数据已提交；处理器必须幂等。

## 3. 模块生命周期钩子

**动机**：`OnApplicationInitializationAsync` / `OnApplicationShutdownAsync` 原本永不执行（`IModuleInitializer` 注册后无人解析），且 `ModuleInitializer` 使用的是未排序的模块列表。

**实现**：`IModuleInitializer` / `ModuleInitializer` 移到 `Frame.Core.Modules` 并公开；`RegisterModules` 返回排序后的 `List<Type>`；`RegisterInfrastructure` 用排序结果构造 `ModuleInitializer`，并 `TryAddEnumerable` 注册 `ModuleInitializationHostedService`（`IHostedService`）：启动正序初始化、停止逆序关闭。

**残留限制**：

- 依赖宿主执行 `IHostedService`；裸 `ServiceProvider` 不会触发；
- 初始化异常会让宿主启动失败（没有「跳过失败模块」的开关）。

## 4. 延迟持久化与自动提交

**动机**：EF Core 通道的 `Insert/Update/Delete` 只做变更追踪并返回 `0`，而 `DatabaseContext` 没有暴露 `SaveChanges`，导致「写了但没落库」。

**实现**：`DatabaseContext` 把 `IDbContext` 挂到 `IUnitOfWork.Items[DbContextItemKey]`，注册完成处理器：收集事件 → `BeginTransactionAsync` → `SaveChangesAsync` → `CommitAsync` → 分发事件 → 清空事件；回滚处理器调用 `IDbContext.Rollback()`。

**行为变更**：`DeleteBatch` 从「直接 Dapper `DELETE ... WHERE Id IN @ids` 并返回行数」改为「逐个 `Remove` 并返回 `0`」，与单条 `Delete` 的延迟语义一致。依赖旧返回值的代码需要调整。

**残留限制**：

- 没有活动 UOW 时不会自动提交，仍需手工 `SaveChangesAsync`（例如在纯查询服务里写数据）；
- 一个 UOW 内多个 `DatabaseContext` 实例各自注册处理器，提交顺序按注册顺序；
- 事务隔离级别使用提供程序默认值，`UnitOfWorkOptions` 没有隔离级别字段。

## 5. `IDbContext` 的获取方式

`DatabaseContext` 有意不暴露 `GetDbContext()` / `SaveChanges()`：外部只能 `GetRepository<T>()`，由框架保证「同一个 Scope、同一个连接串、同一个变更追踪器」。

后果：

- 直接注入 `IDbContext` 会抛「连接字符串未设置」——父 Scope 的 `DbContextConnectionStringAccessor` 没有被赋值；
- 需要 `SaveChanges` 时，在业务仓储里加转发方法：

```csharp
public interface IUserRepository : IRepository<int, User>
{
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
}

public sealed class UserRepository : Repository<int, User>, IUserRepository
{
    public Task<int> SaveAsync(CancellationToken cancellationToken = default)
        => DBContext.SaveChangesAsync(cancellationToken);
}
```

- `DbContextAccessor`（静态）仍保留但已 `[Obsolete]`，新代码不要用。

## 6. 模块排序与环检测

`SortModulesByDependency` 是 DFS 后序：先标记 `visited` 再递归依赖，最后把自身加入结果。它能保证「被依赖者在前」，但：

- 同一层模块的相对顺序取决于反射返回顺序，不稳定；
- 环依赖不会抛错，只是顺序退化；
- 只识别 `[DependsOn]`，不识别构造函数/服务依赖。

需要稳定顺序时，在 `[DependsOn]` 里显式声明，或把注册拆到不同阶段。

## 7. Redis 上下文释放关闭全局连接（已修复）

**当时的问题**：`RedisDbContext.Dispose` 调用 `_redis.Close()`，而 `ConnectionMultiplexer` 是 Singleton 注册的。Scoped 的 `RedisContext` 被释放时会把全局连接关掉，后续请求复用同一个 multiplexer 就会失败（第一次请求正常，第二次开始报 `ObjectDisposedException`）。

**现状**：`RedisDbContext.Dispose()` 已改为空实现，只保留注释说明「multiplexer 的生命周期由 DI 容器的 Singleton 管理」。`RedisContext.Dispose` 仍然释放自己持有的 `RedisDbContext`，但不再触碰连接。

**残留边界**：

- 应用停止时由容器释放 `ConnectionMultiplexer`，没有显式的优雅关闭；
- 若同一个进程里手动 `new` 多个 multiplexer，需要自己负责释放；
- `UseRedisDatabase` 在**注册阶段**就同步 `ConnectionMultiplexer.Connect(...)`，Redis 不可用会直接让启动失败（这属于有意的快速失败，但会拖慢启动）。

## 8. 方言静态缓存与可切换性

`EfCoreDbContext.GetTableName` 用 `static ConcurrentDictionary<Type,string>` 缓存**已加引号**的表名。一旦用 `MySqlDialect` 缓存过，再切换成 `SqlServerDialect` 仍会拿到反引号表名。

**修复方向**：缓存键加上方言 `Name`，或缓存未加引号的 `(schema, table)`。当前默认单方言，风险可控。

## 9. 事件总线的可靠性边界

- 队列是 `Channel.CreateUnbounded`，进程内、无持久化；进程退出时未消费的事件丢失；
- 单读者串行消费，一个慢处理器会拖慢后续事件；
- 重试 3 次、指数退避（1s/2s/4s），最终失败只记日志并丢弃；
- 按运行期具体类型分发，父类/接口事件不会路由到子类处理器；
- 一个类实现多个 `IEventHandler<T>` 时 `GetInterface` 只取一个。

需要「至少一次」语义时，用 outbox 表或外部队列，不要依赖本事件总线。

## 10. 其他小落差

| 位置 | 现状 |
| --- | --- |
| `samples/*/appsettings.json` | 连接串是占位值（`your-password`），首次运行前必须改成真实 MySQL / Redis 地址 |
| 三个示例的表结构 | 框架不带迁移，`Users` / `Projects` / `TaskItems` 需要手工建表（SQL 见 [示例说明](../guide/samples.md#5-数据库与表结构)） |
| `samples/Scheduler/Job/Program.cs` | `UseScheduler()` 的无参调用被注释，只有 `[Scheduled]` 的 `Test3Scheduler` 不会注册；属性发现的实际用例见 TaskBoard Worker |
| `samples/TaskBoard/Worker` | Quartz 使用内存 JobStore，进程重启后作业重新注册、错过的触发不会补跑 |
| `Frame.Locks` `LockState` | 枚举已定义但 `ILock` 只用 `bool IsAcquired` |
| `Frame.Redis` / `Frame.Databases` | 各自定义了一个同名 `ConnectionStringStrategy` 枚举，容易混用 |
| `Frame.Databases/Enums` | `UseDatabaseContext` 用 `configuration.Add` 而非 `TryAdd`，后注册覆盖先注册 |
| `Frame.Core/Caches`、`Loggers` | csproj 里 `Compile Remove` 的残留目录，源码已不存在 |
| `Frame.Core` 的 `Utils` 空目录 | csproj 仍声明，可删 |
| `ApplicationService` 无参构造 | 把 `UnitOfWorkManager` 置 `null!`，调用 `BeginUnitOfWork` 会 NRE |

## 11. 如果只能做三件事

1. **outbox**：把领域事件与业务数据写进同一事务，后台补偿投递，解决「已提交未分发」；
2. **反转 Application → Infrastructure 依赖**：示例里应用服务要注入 `XxxDatabaseContext` 才能让框架把 `IDbContext` 挂到 UOW 上，理想形态是只依赖仓储接口（需要框架把「解析仓储」也纳入 UOW 挂接点）；
3. **Dapper 通道的审计与软删除过滤**：目前审计/软删除只覆盖 EF Core 通道，Dapper 查询会返回已删除数据。

三项都涉及行为变更，动手前先补集成测试。

相关文档：[整体架构](../concepts/architecture.md) · [工作单元与自动事务](../concepts/unit-of-work.md) · [领域事件](../concepts/domain-events.md) · [审计字段](../concepts/auditing.md) · [模块与目录结构](../concepts/modules.md)
