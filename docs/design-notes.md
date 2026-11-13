# 设计说明与已知落差

这份文档记录「框架想做什么」与「代码实际做了什么」之间的差距，以及为什么会出现这些差距。它不是 bug 列表，而是给维护者的决策依据：有些落差是刻意的简化（未接线但接口已留好），有些是遗漏，有些是明确的实现缺陷。

## 0. 设计目标 vs 现状

| 设计目标 | 现状 | 结论 |
| --- | --- | --- |
| 模块化装配、零配置发现 | ✅ 已实现：BFS 扫程序集 + `[DependsOn]` 拓扑 + `[ServiceModule]` | 可用 |
| 统一工作单元 | ⚠️ 接口完整，但 `[UnitOfWork]` 无拦截器、`RegisterDbActions` 空实现 | 需手工管理 |
| 领域事件随 UOW 提交分发 | ❌ 只有 `Entity.DomainEvents` 容器，无分发器 | 未接线 |
| 审计字段自动填充 | ❌ 只有 XML 注释里提到接口名，类型不存在 | 未实现 |
| Dapper + EF Core 双通道 | ✅ 已实现，可在同一 `IDbContext` 上混用 | 可用 |
| 多数据库方言 | ✅ MySQL / SQL Server / PostgreSQL 的 CRUD 与分页 | 可用，切换有坑 |
| 分布式锁 | ✅ Local + Redis 两套实现 | 可用，Redis 需注意生命周期 |
| 进程内事件总线 | ✅ 已实现，异步、重试 | 可用，不可靠 |
| 定时任务 | ✅ Quartz 包装 + 特性/配置注册 | 可用，内存 JobStore |
| Redis 缓存访问 | ✅ 命令封装完整 | 有 Scope 释放关闭连接的缺陷 |

## 1. `[UnitOfWork]` 没有拦截器

**期望**：在应用服务方法上标 `[UnitOfWork]`，方法进入时自动 `Begin`、正常返回时 `Complete`、抛异常时 `Rollback`。

**实际**：`UnitOfWorkAttribute` 只是一个普通特性：

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class UnitOfWorkAttribute : Attribute
{
    public bool IsAutoRollback { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 0;
    public TimeSpan? Timeout => TimeoutSeconds > 0 ? TimeSpan.FromSeconds(TimeoutSeconds) : null;
}
```

全仓库没有任何 AOP 代理、`IInterceptor`、`DispatchProxy` 或 MVC 过滤器读取它。标注后**什么也不会发生**。

**影响**：不 `Begin` 就没有 `UnitOfWorkManager.Current`，`ApplicationService.CompleteUnitOfWorkAsync` 直接返回；嵌套 UOW、超时、自动回滚全部不生效。

**当前可行做法**：在应用服务里显式写：

```csharp
public async Task DoAsync()
{
    using var uow = BeginUnitOfWork(new UnitOfWorkOptions { IsAutoRollback = true });
    try
    {
        // 业务
        await CompleteUnitOfWorkAsync();
    }
    catch
    {
        await uow.RollbackAsync();
        throw;
    }
}
```

**修复建议**：实现一个 `DispatchProxy`/Castle 动态代理，在 `UseApplication` 注册时把带 `[UnitOfWork]` 的类型包一层；或在 `AddFrameService` 里注册一个 MVC `IAsyncActionFilter` 作为轻量替代。前者能覆盖非 Web 场景，后者实现成本低但只覆盖控制器。

## 2. 领域事件没有分发器

**期望**：`Entity.AddDomainEvent(...)` 之后，UOW 提交前自动把事件交给 `IEventBus` 或 `IEventHandler`。

**实际**：`Entity` 只维护一个 `List<object>?`：

```csharp
public IReadOnlyList<object>? DomainEvents => _domainEvents;
protected void AddDomainEvent(object eventItem) { ... }
protected void RemoveDomainEvent(object eventItem) { ... }
protected void ClearDomainEvents() { ... }
```

`grep DomainEvents` 只命中 `Entity.cs` 自身。`UnitOfWork` / `UnitOfWorkManager` / `Repository` / `DatabaseContext` 都没有读取它，`Frame.EventBus` 也不知道实体事件的存在。

**影响**：`AddDomainEvent` 是死代码；即使调用，事件也不会离开实体对象，更不会在事务边界内/外分发。

**修复建议**：在 UOW 完成前遍历 `DbContext.ChangeTracker.Entries()` 中实现 `IEntity` 的实例，收集 `DomainEvents`，依次 `await eventBus.Push(...)`，再 `ClearDomainEvents()`。注意此时 `eventBus.Push` 是「入队即返回」，与「事务提交后再分发」的语义差异——要真正保证「提交成功才发事件」，需要 outbox 表。

## 3. 模块生命周期方法永不执行

**期望**：模块的 `OnApplicationInitializationAsync` 在应用启动时执行、`OnApplicationShutdownAsync` 在停止时执行（用于预热缓存、注册后台任务等）。

**实际**：`ServiceDependencyInjection.RegisterInfrastructure` 里：

```csharp
services.AddSingleton<IModuleInitializer>(sp => new ModuleInitializer(modules));
```

`IModuleInitializer` 是 `internal` 接口，`ModuleInitializer` 也是内部类，**全仓库没有任何地方解析它**，更没有代码调用 `OnApplicationInitializationAsync`。启动流程在 `AddFrameService` 返回后就结束了。

**影响**：模块里写的初始化逻辑静默不执行；`FrameModule` 的五个方法里只有三个 `Configure*` 真正被调用。

**修复建议**：注册一个 `IHostedService`，在 `StartAsync` 里解析 `IModuleInitializer` 并依次调用初始化；`StopAsync` 逆序调用关闭。注意 `ModuleInitializer` 当前用的是模块的**原始声明顺序**而不是拓扑序，需要一并修正（见第 6 条）。

## 4. 延迟持久化没有自动提交

**期望**：`Insert/Update/Delete` 立即生效，或由 UOW 统一提交。

**实际**：EF Core 通道的写方法只调用 `Add/Update/Remove` 并 `return 0`，注释写着「延迟持久化，返回 0 表示待提交」；真正落库要 `SaveChanges/SaveChangesAsync`。而 `UnitOfWorkManager.RegisterDbActions(onComplete: async ct => { await Task.CompletedTask; })` 是个空回调，没有任何自动提交。

**影响**：按「DDD 仓储」直觉写代码会静默丢数据——没有异常，只是数据库里没有。

**修复建议**：在 `RegisterDbActions` 的 `onComplete` 里解析 `IDbContext` 并 `SaveChangesAsync`。难点是 UOW 完成时代码可能已经离开 `DatabaseContext` 的子 Scope，拿不到同一个 `IDbContext` 实例；需要把 `IDbContext` 挂到 UOW 上（例如 `UnitOfWork` 增加 `Items` 字典）。

## 5. `DatabaseContext` 没有暴露 `IDbContext`

**期望**：`DatabaseContext` 作为「数据库会话」，能直接 `SaveChanges` / 开事务 / 执行 SQL，或至少 `GetDbContext()` 返回内部实例。

**实际**：只有两个 `GetRepository` 方法和 `Initialize`。内部的 `IDbContext` 藏在私有 `IServiceScope` 里，通过 `DbContextScope`（`AsyncLocal`）给仓储用。

**影响**：

- 想 `SaveChanges` 只能绕道仓储的 `protected IDbContext DBContext`；
- 想开事务同样只能绕道；
- 直接注入 `IDbContext` 拿到的是父 Scope 的实例，而父 Scope 的 `DbContextConnectionStringAccessor.ConnectionString` 从未赋值，构造时抛「连接字符串未设置」——这是一个容易踩的陷阱。

**修复建议**：给 `DatabaseContext` 加 `public IDbContext DbContext => GetOrCreateScope()...` 或 `SaveChangesAsync()` 转发，并在父 Scope 上同步设置连接串访问器，让直接注入 `IDbContext` 也可用。这是收益最高的一处改动。

## 6. 模块排序是 DFS 后序，不是稳定拓扑序

`SortModulesByDependency` 用递归 DFS，`visited` 在进入时标记，因此：

- **不会检测环**：`A → B → A` 会静默得到某个顺序，而不是报错；
- 顺序依赖声明顺序：同一层的模块按程序集/类型枚举顺序排列，不完全确定；
- `ModuleInitializer` 构造时使用的是**未排序的** `moduleTypes`，与 `RegisterModules` 的排序结果不一致。

**影响**：模块间的依赖顺序在复杂图上可能不满足「被依赖者先初始化」；初始化逻辑一旦接线（第 3 条），这个隐患会立刻暴露。

## 7. Redis 上下文释放关闭全局连接

`RedisDbContext.Dispose()` 调 `_redis.Close()`，而 `_redis` 来自 Singleton 的 `ConnectionMultiplexer`。Scoped `RedisContext` 一释放，整个应用的连接就断了。

这是实现缺陷而非设计取舍：`RedisDbContext` 只是 `GetDatabase()` 的持有者，不该拥有连接的生命周期。修复方式是把 `Close()` 移出 `Dispose`，或在 `RedisContext` 里不释放内部 `RedisDbContext`。

## 8. 方言静态缓存与可切换性

`EfCoreDbContext.GetTableName` 用 `static ConcurrentDictionary<Type, string>` 缓存**已经加引号**的表名。方言是实例属性（默认 MySQL），一旦某个类型被 MySQL 方言缓存过，再切到 SQL Server 也会拿到反引号表名。

同时 `CreateDialect` 是 `protected internal virtual`，用户子类可覆盖；但 `UseDatabaseContext<T>` 的 `T` 只约束 `DatabaseContext, new()`，无法指定方言，所以「切换数据库」实际要改 `DbContext` 子类。

## 9. 事件总线的可靠性边界

`Push` 写入无界通道后立即返回，消费循环单线程串行处理：

- 进程退出时通道里的事件丢失；
- Handler 重试耗尽后丢弃；
- 没有多态分发（只按 `GetType()` 精确匹配）；
- 慢 Handler 阻塞全局。

这些是「进程内轻量总线」的合理取舍，但**不能**当消息队列用。需要可靠投递时应在业务侧落 outbox 表，或换 `Frame.EventBus` 之外的实现。

## 10. 其他小落差

| 位置 | 落差 |
| --- | --- |
| `Entity.cs` XML 注释 | `<see cref="IHasCreationTime"/>` 等 4 个审计接口不存在（CS1574 警告） |
| `IDBContext.SaveChanges` 注释 | 写「通常 Insert/Update/Delete 已自动保存」，与实现相反 |
| `Frame.Core.csproj` | `Compile Remove="Caches\**"`、`Loggers\**`，且有一个空的 `Folder Utils\`——属于未完成的分层残留 |
| `DatabaseContextConfiguration` | 用 `Add` 而非 `TryAdd`，重复注册会覆盖，`GetServices<IDbContext>()` 可能拿到多个 |
| `ServiceApplicationConfiguration` | 一个类实现多个 `IApplicationService` 时只注册第一个接口 |
| `LockState` | 枚举定义了 5 个状态，`ILock` 只用 `bool IsAcquired` |
| samples `Program.cs` | Command/Query 的连接串传反；`appsettings.json` 缺连接串配置节，按现配置启动必抛异常 |
| samples `Job/Program.cs` | `UseScheduler()` 被注释，只走配置列表；`Test3Scheduler` 的 `[Scheduled]` 因此不生效 |
| `Web/Filters/ModelValidationFilter.cs` | 文件名叫 Filter，实际是 `ModelValidationAttribute : ActionFilterAttribute` |

## 11. 如果只能做三件事

1. **给 `DatabaseContext` 暴露 `IDbContext` / `SaveChangesAsync`**（第 5 条）——解开「无法落库」的死结；
2. **接线 `IModuleInitializer` 并通过 `IHostedService` 驱动**（第 3 条）——让模块生命周期生效；
3. **修 `RedisDbContext.Dispose`**（第 7 条）——避免第二次请求就崩。

之后再考虑 `[UnitOfWork]` 拦截器（第 1 条）与领域事件分发（第 2 条），因为它们依赖前两项提供的事务与初始化能力。
