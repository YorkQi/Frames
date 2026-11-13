---
title: 整体架构
audience: 贡献者
status: stable
updated: 2026-11-13
sources:
  - src/Frame.Core/ServiceDependencyInjection.cs
  - src/Frame.Core/Modules
  - src/Frame.Core/Uow
---

# 整体架构

本文描述框架的内部结构、启动装配流程、依赖注入生命周期和一次请求的调用链路。想直接上手可以跳过，改框架代码前建议通读。

## 1. 设计目标

| 目标 | 落地方式 | 代价 |
| --- | --- | --- |
| 业务代码不感知基础设施 | 领域层只依赖 `Frame.Core` 的实体/仓储契约；读写通过 `DatabaseContext.GetRepository<T>()` 拿到的接口进行 | 仓储实例必须来自 `DatabaseContext` 作用域，否则 `DbContextScope.Current` 为空 |
| 少写装配代码 | 入口程序集 BFS 发现业务程序集，按标记接口/特性扫描注册 | 只扫 `public` 类型；框架程序集按前缀硬编码排除 |
| 一套实体同时支持两类查询 | EF Core 负责变更追踪，Dapper 负责手写 SQL/批量/分页（`IDbContext` 上的 `partial class`） | 写操作延迟到 UOW 提交（或手工 `SaveChanges`）才落库 |
| 横切能力可插拔 | 锁、事件总线、调度、Redis 各自提供 `UseXxx()` 扩展方法 | 没有统一的横切管线，也就没有 AOP（`[UnitOfWork]` 因此未生效） |
| 小核心 | `Frame.Core` 仅依赖 `Microsoft.Extensions.DependencyInjection.Abstractions` / `Logging.Abstractions` / `Hosting.Abstractions` | 核心不含多租户、权限等常见 DDD 构件（审计与软删除只有契约与填充器） |

## 2. 分层

框架内部按「契约 → 实现 → 适配」分三层，业务工程再按 DDD 分五层。

框架侧：

```
Frame.Core            契约与装配（无外部实现依赖）
   ├─ Frame.Databases  EF Core + Dapper 实现（net8.0）
   ├─ Frame.Locks      Monitor / RedLock 实现
   ├─ Frame.EventBus   Channel 实现
   ├─ Frame.Scheduler  Quartz 适配
   └─ Frame.Redis      StackExchange.Redis 适配
```

业务侧（`samples/Webs`）：

```
Web ──► Application ──► Infrastructure ──► Repository ──► Domain ──► Frame.Core
                          │                    │
                          ├─ Frame.EventBus    └─ Frame.Databases
                          ├─ Frame.Locks
                          └─ Frame.Redis
```

- `Web` 只引用 `Application`，控制器注入 `IApplicationService` 与基础设施上下文；
- `Application` 引用 `Infrastructure`，应用服务通过 `CommandDatabaseContext` / `QueryDatabaseContext` 获取仓储；
- `Domain` 只引用 `Frame.Core`，放实体、枚举、仓储接口、应用服务接口；
- 依赖方向单向，`Domain` 不知道数据库存在。

> 注意：`Application` 直接依赖 `Infrastructure` 是示例的简化。更严格的做法是让应用服务只依赖 `IUnitOfWorkManager` + 领域仓储接口，由 `UseDatabaseContext` 负责实现绑定；但框架的 `GetRepository<T>()` 模式要求能拿到 `DatabaseContext`，示例选择了务实写法。

## 3. 启动装配：`AddFrameService`

`ServiceDependencyInjection.AddFrameService` 分四步，顺序固定：

```csharp
var assemblies = GetApplicationAssemblies();          // ① BFS 发现
var ctx = new ServiceConfigurationContext(assemblies);
configuration?.Invoke(ctx);                           // ② 用户配置
RegisterModules(ctx);                                 // ③ 模块拓扑与生命周期
services.Add(ctx.GetServiceDescriptors());            // ④ 写入容器
RegisterInfrastructure(services, ctx);                // ⑤ 基础设施补齐
```

### 3.1 程序集发现（BFS）

```csharp
entryAssembly = Assembly.GetEntryAssembly();          // 可能为 null（如单元测试宿主）
queue: entryAssembly
while queue:
    result.Add(assembly)
    foreach referenced in assembly.GetReferencedAssemblies():
        if 已访问: continue
        标记已访问
        if 名称以 System / Microsoft / netstandard / mscorlib 开头: continue
        try { queue.Enqueue(Assembly.Load(referenced)) } catch { /* 忽略 */ }
```

要点：

- **动态加载**：引用关系来自元数据，`Assembly.Load` 失败会被吞掉。反射 dynamic 加载的程序集、`Assembly.LoadFrom` 在运行时加载的程序集不在发现范围内。
- **只进不出的排除**：`Frame.*` 自身不会被排除，因此框架程序集也会被扫描（它们没有 `[ServiceModule]`、`IApplicationService`、`IRepository` 实现，通常无副作用）。
- 需要额外程序集时用 `context.AddAssembly(assembly)` 或 `context.AddAssemblyOf<T>()`，必须在 `AddFrameService` 的回调里调用。
- 单元测试里 `Assembly.GetEntryAssembly()` 通常指向 testhost，发现的程序集与生产不同——这是测试与集成测试环境差异的主要来源。

### 3.2 类型可见性

`ServiceConfigurationContext.GetAssemblyTypes()` 返回 `Assembly.GetExportedTypes()`，即**只有 public 类型**。所有扫描注册都建立在它之上：

- `internal` / `private` 的模块、应用服务、仓储不会被注册，也不会报错；
- 泛型开放类型（如 `Repository<,>`）无法作为服务实现注册，必须写具体子类。

### 3.3 模块拓扑

`SortModulesByDependency` 是 **DFS 后序** 拓扑排序（不是 Kahn）：

```
dfs(type):
    if visited: return
    visited.add(type)                  // 先标记，天然避免环导致的无限递归
    foreach [DependsOn] 中的依赖 dep:
        if dep 在模块集合内: dfs(dep)
    sorted.add(type)                   // 依赖先入列
```

结果：

- `PreConfigureServices` 正序（父模块先）；
- `ConfigureServices` 正序；
- `PostConfigureServices` 逆序（子模块先）；
- 依赖环不会抛异常，只是顺序退化为「首次访问顺序」，需自行避免。

模块实例在 `PreConfigureServices` 阶段用 `Activator.CreateInstance` 创建一次，并以 `Singleton` 注册自身类型；后续两个阶段从容器描述符里取回同一实例。

### 3.4 基础设施补齐

```csharp
services.TryAddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
services.TryAddSingleton<IClock, SystemClock>();
services.TryAddScoped<ICurrentUser, NullCurrentUser>();
services.TryAddScoped<IAuditPropertySetter, AuditPropertySetter>();
if (存在模块)
{
    services.AddSingleton<IModuleInitializer>(sp => new ModuleInitializer(sortedModuleTypes));
    services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ModuleInitializationHostedService>());
}
```

`ModuleInitializationHostedService` 在宿主启动时正序调用各模块的 `OnApplicationInitializationAsync`，停止时逆序调用 `OnApplicationShutdownAsync`。它依赖宿主执行 `IHostedService`（ASP.NET Core / Generic Host 默认会执行）。详见 [模块与目录结构](modules.md#33-生命周期钩子)。

## 4. 依赖注入生命周期

| 服务 | 生命周期 | 注册位置 | 说明 |
| --- | --- | --- | --- |
| `IUnitOfWorkManager` | Scoped | `AddFrameService` 尾部 `TryAddScoped` | 用户可提前注册自己的实现覆盖 |
| `IClock` / `ICurrentUser` / `IAuditPropertySetter` | Singleton / Scoped / Scoped | 同上 `TryAdd*` | 审计字段的时间与用户来源，可替换 |
| `IModuleInitializer` / `ModuleInitializationHostedService` | Singleton | `AddFrameService` 尾部 | 仅在存在 `IFrameModule` 时注册 |
| `IFrameModule` 实现 | Singleton（自身类型） | `RegisterModules` | 仅模块实例，接口 `IFrameModule` 未注册 |
| `IApplicationService` 派生接口 | Scoped | `UseApplication` | 每个类只注册**第一个**匹配接口 |
| `DbContextConnectionStringAccessor` | Scoped | `UseDatabaseContext` | 持有本次请求选中的连接串 |
| `IDbContext` | Scoped（工厂） | `UseDatabaseContext` | `new DbContext(accessor)` 并 `Dialect = CreateDialect()` |
| `DatabaseContext` 子类 | Scoped | `UseDatabaseContext` | `new` + `Initialize`，内部再建子 Scope |
| 仓储（`IRepository` 派生接口） | Scoped | `UseDatabaseContext` → `UseRepositories` | 需为具体类 |
| `ILockFactory` | Singleton | `UseLock` | 内部持有 RedLock 工厂或静态字典 |
| `IEventBus` | Singleton | `UseEventBus` | `LocalEventBus`，单读者通道 |
| `IHostedService`（事件总线） | Singleton | `UseEventBus` | `EventBusHostService` |
| `IJobFactory` / `ISchedulerFactory` / `IFrameScheduler` | Singleton | `UseScheduler` → `UseQuartz` | Quartz 调度器单例 |
| `JobSchedule` | Singleton | `UseScheduler` | 每个作业一条 |
| `ISchedulerJob` 实现 | Scoped | `UseScheduler` | 每次触发新建 Scope |
| `ConnectionMultiplexer` | Singleton | `UseRedisDatabase` | **注册时同步连接** |
| `RedisContext` 子类 | Scoped | `UseRedisDatabase` | 懒建 `RedisDbContext` |

补充：

- `IDbContext` 是 Scoped，但 `DatabaseContext` 在 Scoped 内部又 `CreateScope()`，所以一次请求里 `DatabaseContext` 及其子 Scope 共享同一个 `IDbContext`，而不同 `DatabaseContext` 子类（读写分离）各自独立。
- `UseLock` 的工厂是 Singleton，`RedisLockFactory` 内部持有多个 `ConnectionMultiplexer`，随容器释放。
- `UseRedisDatabase` 的连接在**注册阶段**同步建立，应用启动会被阻塞；连接失败直接抛异常。

## 5. 一次请求的调用链

```
HTTP Request
  └─ Controller（注入 IApplicationService / DatabaseContext / IEventBus / ILockFactory）
       └─ IUserService 代理（[UnitOfWork] → UnitOfWorkInterceptor：Begin）
            └─ UserService（Scoped，真实实现）
                 └─ CommandDatabaseContext.GetRepository<IUserRepository>()
                      ├─ 首次调用：CreateScope()
                      │    ├─ 选中连接串 → DbContextConnectionStringAccessor
                      │    ├─ 解析 IDbContext（Scoped，EF Core + Dapper 双通道）
                      │    └─ DbContextScope.Set(dbContext)      // AsyncLocal
                      ├─ AttachToUnitOfWork（挂 IDbContext + 注册提交/回滚处理器）
                      └─ Repository（Scoped）
                           └─ IDbContext.InsertAsync / QueryAsync（写操作只挂起，返回 0）
                                └─ EF Core / Dapper → MySQL
            └─ 代理：CompleteAsync
                 └─ 收集事件 → BeginTransaction → SaveChanges → Commit → DomainEventDispatcher
       └─ IEventBus.Push(event)  →  Channel → EventBusHostService → Handler（新 Scope）
```

要点：

1. `DbContextScope` 是 `AsyncLocal`，**跨 `Task.Run` / 线程切换时可能丢上下文**；仓储若在后台线程创建，`DBContext` 会抛「IDbContext 不可用」。
2. 仓储优先用构造函数注入的 `IDbContext`，没有才读 `AsyncLocal`。示例中的仓储走的是后者。
3. 事件总线的 Handler 每次从新 Scope 解析，因此 Handler 里可以安全注入 Scoped 服务；但事件处理与请求**不在同一事务/同一个 `DatabaseContext`** 里。
4. 锁与事务无关：`ILock` 只是互斥，不参与数据库事务。

## 6. 模块系统

### 6.1 组成

| 构件 | 作用 | 现状 |
| --- | --- | --- |
| `IFrameModule` | 5 个钩子：Pre/Configure/Post + 应用初始化/关闭 | 全部生效（后两个经 `IHostedService`） |
| `FrameModule` | 空实现基类 | 可用 |
| `[DependsOn(params Type[])]` | 声明模块依赖，决定执行顺序 | 生效（DFS 后序） |
| `[ServiceModule(ServiceLifetime)]` | 标记类为「按生命周期注册自身」 | 生效，但**只注册自身类型**，不映射接口 |
| `IModuleInitializer` | 应用启动/关闭时调用模块钩子 | ✅ 由 `ModuleInitializationHostedService` 调用 |

### 6.2 典型写法

```csharp
[DependsOn(typeof(InfrastructureModule))]
public sealed class ApplicationModule : FrameModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.AddAssemblyOf<ApplicationModule>();
    }
}

[ServiceModule(ServiceLifetime.Scoped)]
public sealed class OrderDomainService { /* 构造注入即可 */ }
```

注意事项：

- `[ServiceModule]` 注册的是 `ServiceDescriptor(type, lifetime)`，即 `services.AddScoped<OrderDomainService>()`。若需要接口映射，请在该模块的 `ConfigureServices` 里手工 `context.Add(...)`。
- 模块类必须是 `public`，否则 `GetExportedTypes()` 看不到。
- `[DependsOn]` 指向的模块若不在发现集合内会被静默忽略（不会报错，也不会自动加载）。

## 7. 横切能力的装配位置

| 能力 | 扩展方法 | 装配产物 | 是否影响请求管线 |
| --- | --- | --- | --- |
| 工作单元 | 无（`ApplicationService` 提供 `BeginUnitOfWork`） | `IUnitOfWorkManager` | 否 |
| 数据库 | `UseDatabaseContext<T>` | `IDbContext`、`DatabaseContext`、仓储 | 否 |
| 锁 | `UseLock` | `ILockFactory` | 否 |
| 事件总线 | `UseEventBus` | `IEventBus` + `IHostedService` | 后台消费者 |
| 调度 | `UseScheduler` | Quartz 调度器 + `IHostedService` | 后台触发 |
| Redis | `UseRedisDatabase<T>` | `ConnectionMultiplexer` + `RedisContext` | 否 |

框架不注册任何 MVC 过滤器、中间件或 Action 拦截器。示例里的 `ExceptionFilter` / `ApiExceptionFilter` 属于示例代码，不是框架能力。

## 8. 硬约束

1. **`Frame.Databases` 只能是 `net8.0`**：EF Core 8 与 Pomelo 8 的要求；其余程序集 `netstandard2.1`，可被更老的运行时引用。
2. **没有配置系统绑定**：所有 `UseXxx` 接收的是强类型对象，配置文件读取由应用自己完成（示例用 `IConfiguration.GetRequiredSection`）。
3. **没有 AOP/动态代理**：任何「特性驱动」的行为（`[UnitOfWork]`、`[Scheduled]` 除外）都需要调用方显式执行。
4. **扫描只针对 public 类型**，且只针对 BFS 可达程序集。
5. **SQL 方言只在 `DbContext` 内生效**：默认 MySQL；切换到 SQL Server/PostgreSQL 需要自定义 `DbContext` 子类并重写注册流程（见 [databases.md](databases.md#8-分页与方言)）。
