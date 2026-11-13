# 模块与目录结构

本文回答两件事：**框架由哪些模块组成、每个模块的代码放在哪**；以及**模块系统如何发现、排序、初始化模块**。

## 1. 程序集总览

| 程序集 | TFM | 依赖 | 对应文档 |
| --- | --- | --- | --- |
| `Frame.Core` | `netstandard2.1` | DI/Logging 抽象、`System.Text.Json` | [core.md](core.md) · [unit-of-work.md](unit-of-work.md) · [auditing.md](auditing.md) · [domain-events.md](domain-events.md) |
| `Frame.Databases` | `net8.0` | `Frame.Core`、Dapper、EF Core 8、Pomelo MySQL | [databases.md](databases.md) |
| `Frame.Locks` | `netstandard2.1` | `Frame.Core`、RedLock.net | [locks.md](locks.md) |
| `Frame.EventBus` | `netstandard2.1` | `Frame.Core`、`System.Threading.Channels` | [eventbus.md](eventbus.md) |
| `Frame.Scheduler` | `netstandard2.1` | `Frame.Core`、Quartz.AspNetCore、NCrontab | [scheduler.md](scheduler.md) |
| `Frame.Redis` | `netstandard2.1` | `Frame.Core`、StackExchange.Redis | [redis.md](redis.md) |

依赖方向：所有模块只依赖 `Frame.Core`，**模块之间不互相引用**（`Frame.Databases` 通过 `Frame.Core` 的 `IDomainEventDispatcher` 抽象与事件总线解耦，具体实现在宿主装配时注入）。

```
Frame.Core  ←  Frame.Databases
            ←  Frame.Locks
            ←  Frame.EventBus
            ←  Frame.Scheduler
            ←  Frame.Redis
```

## 2. 各模块目录结构

### 2.1 Frame.Core

```
src/Frame.Core/
├─ Application/
│  ├─ IApplicationService.cs            应用服务标记接口
│  ├─ ApplicationService.cs             基类：BeginUnitOfWork / CompleteUnitOfWorkAsync
│  └─ ServiceApplicationConfiguration.cs UseApplication()：扫描并按接口注册（含 UOW 代理）
├─ Auditing/
│  ├─ AuditingInterfaces.cs             IHasCreationTime / ISoftDelete / IFullAuditedObject 等
│  ├─ IClock.cs                         IClock + SystemClock
│  ├─ ICurrentUser.cs                   ICurrentUser + NullCurrentUser
│  └─ AuditPropertySetter.cs            IAuditPropertySetter + 默认实现
├─ DomainEvents/
│  └─ IDomainEventDispatcher.cs         领域事件分发契约
├─ Entities/
│  ├─ IEntity.cs                        IEntity / IEntity<TKey>
│  ├─ Entity.cs                         实体基类：相等性、领域事件容器
│  ├─ IHasDomainEvents.cs               只读事件视图 + 清空
│  └─ Dtos/
│     ├─ PageInput.cs                   Page / Limit
│     └─ PageResult.cs                  Items / Count
├─ Modules/
│  ├─ IFrameModule.cs                   模块契约（5 个钩子）
│  ├─ FrameModule.cs                    空实现基类
│  ├─ DependsOnAttribute.cs             声明模块依赖
│  ├─ ServiceModuleAttribute.cs         按生命周期注册模块自身
│  ├─ ServiceModuleConfiguration.cs     UseModule()：扫描 [ServiceModule]
│  ├─ ModuleInitializer.cs              IModuleInitializer：按序初始化/逆序关闭
│  └─ ModuleInitializationHostedService.cs 把初始化钩子接到 IHostedService
├─ Repositories/
│  └─ IRepository.cs                    IRepository / IRepository<TKey,TEntity>
├─ Uow/
│  ├─ IUnitOfWork.cs                    UOW 契约（Items / 处理器 / 提交回滚）
│  ├─ IUnitOfWorkManager.cs             Begin / Current
│  ├─ UnitOfWork.cs                     嵌套、children、处理器顺序、超时
│  ├─ UnitOfWorkManager.cs              AsyncLocal 当前 UOW
│  ├─ UnitOfWorkOptions.cs              IsAutoRollback / Timeout
│  ├─ UnitOfWorkAttribute.cs            [UnitOfWork]
│  └─ Interception/
│     └─ UnitOfWorkProxy.cs             DispatchProxy 拦截器与工厂
├─ ServiceConfigurationContext.cs       装配期注册上下文
├─ ServiceDependencyInjection.cs        AddFrameService()：发现、排序、注册
└─ AssemblyInfo.cs                      InternalsVisibleTo("TestUnit")
```

### 2.2 Frame.Databases

```
src/Frame.Databases/
├─ Dialects/
│  ├─ IDatabaseDialect.cs               分页 SQL / 引用符 / 参数前缀
│  ├─ MySqlDialect.cs                   默认方言
│  ├─ SqlServerDialect.cs               OFFSET/FETCH
│  └─ PostgreSqlDialect.cs              LIMIT/OFFSET + 双引号
├─ DbContexts/
│  ├─ DapperDbContext.cs                partial DbContext：SQL / 分页 / 事务
│  ├─ DapperDbContextReader.cs          QueryMultiple 读取器
│  ├─ EfCoreDbContext.cs                partial DbContext：EF CRUD / 审计 / 变更收集
│  └─ DbContextConnectionStringAccessor.cs  Scoped 连接串持有者
├─ EFCore/
│  └─ EfCoreContext.cs                  实体自动发现与 OnModelCreating
├─ Enums/
│  └─ ConnectionStringStrategy.cs       Random / RoundRobin
├─ Repositories/
│  └─ Repository.cs                     IRepository 默认实现，转发到 IDbContext
├─ DatabaseContext.cs                   请求级上下文：GetRepository / 挂接 UOW
├─ DatabaseContextConfiguration.cs      UseDatabaseContext<T>()
├─ IDBContext.cs                        双通道契约
├─ IDBContextReader.cs                  QueryMultiple 结果读取
├─ DbContextScope.cs                    AsyncLocal 当前 IDbContext
├─ DbContextAccessor.cs                 已过时的静态访问器
├─ ConnectionStringCollection.cs        多连接串 + 负载策略
└─ DbParameters.cs                      Dapper 参数封装（含输出参数）
```

### 2.3 Frame.Locks

```
src/Frame.Locks/
├─ Enums/
│  ├─ LockType.cs                       Local / Redis
│  └─ LockState.cs                      预留状态枚举（当前未被 API 使用）
├─ Local/
│  ├─ LocalLock.cs                      Monitor.TryEnter 封装
│  └─ LocalLockFactory.cs               按资源名共享 SyncRoot + 定期清理
├─ Redis/
│  ├─ RedisLock.cs                      RedLock 包装
│  └─ RedisLockFactory.cs               RedLockFactory 创建与释放
├─ ILock.cs                             锁实例契约
├─ ILockFactory.cs                      创建锁
├─ LockConfiguration.cs                 UseLock()
└─ LockOptions.cs                       连接串列表
```

### 2.4 Frame.EventBus

```
src/Frame.EventBus/
├─ Locals/
│  ├─ LocalEventBus.cs                  Channel 队列 + 串行消费 + 重试
│  └─ EventBusDomainEventDispatcher.cs  把领域事件适配成 IEvent 入队
├─ IEvent.cs                            事件标记
├─ IEventHandler.cs                     处理器契约
├─ IEventBus.cs                         Push / Run
├─ EventBusConfiguration.cs             UseEventBus()：扫描处理器
├─ EventBusHostService.cs               BackgroundService 宿主
└─ EventBusQueueParams.cs               队列元素
```

### 2.5 Frame.Scheduler

```
src/Frame.Scheduler/
├─ Quartz/
│  ├─ QuartzJobFactory.cs               从 DI Scope 取作业实例
│  ├─ QuartzJobWrapper.cs               IJob 适配 + Scope 释放
│  ├─ QuartzScheduler.cs                Quartz 调度器封装
│  └─ QuartzSchedulerConfiguration.cs   UseQuartz()
├─ IFrameScheduler.cs                   启动 / 添加 / 关闭
├─ ISchedulerJob.cs                     作业契约
├─ JobSchedule.cs                       作业名 / 类型全名 / Cron
├─ ScheduledAttribute.cs                [Scheduled]
├─ SchedulerConfiguration.cs            UseScheduler()
└─ SchedulerHostedService.cs            启动时注册并启动调度器
```

### 2.6 Frame.Redis

```
src/Frame.Redis/
├─ Enums/
│  └─ ConnectionStringStrategy.cs       Random / RoundRobin（与 Databases 同名但独立）
├─ RedisDbContexts/
│  └─ RedisDbContext.cs                 IRedisDbContext 默认实现
├─ IRedisDbContext.cs                   Key/String/List/Set/Hash/SortedSet/PubSub
├─ RedisContext.cs                      请求级上下文：GetDbContext()
├─ RedisContextConfiguration.cs         UseRedisDatabase<T>()
└─ RedisConnectionStringCollection.cs   多连接串 + 负载策略
```

## 3. 模块系统

### 3.1 契约

```csharp
public interface IFrameModule
{
    void PreConfigureServices(ServiceConfigurationContext context);
    void ConfigureServices(ServiceConfigurationContext context);
    void PostConfigureServices(ServiceConfigurationContext context);
    Task OnApplicationInitializationAsync(CancellationToken cancellationToken = default);
    Task OnApplicationShutdownAsync(CancellationToken cancellationToken = default);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DependsOnAttribute(params Type[] dependedModuleTypes) : Attribute
{
    public Type[] DependedModuleTypes { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ServiceModuleAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; }
}
```

`FrameModule` 是 5 个方法的空实现基类，业务模块继承它即可按需覆盖。

### 3.2 发现与排序

`AddFrameService` 的流程：

1. `GetApplicationAssemblies()`：从 `Assembly.GetEntryAssembly()` 出发 BFS 遍历引用程序集，跳过 `System` / `Microsoft` / `netstandard` / `mscorlib` 前缀；
2. 用户 `configuration` 回调（可 `AddAssembly` / `AddAssemblyOf<T>()` 补扫描）；
3. `RegisterModules`：找出 `IFrameModule` 的 public 非抽象实现，按 `[DependsOn]` 排序；
4. `RegisterInfrastructure`；
5. `services.Add(descriptors)`。

排序是 **DFS 后序**（`SortModulesByDependency`，`internal` 以便测试）：先递归依赖再输出自己，因此被依赖的模块排在前面。它**不做环检测**——模块 A 依赖 B、B 又依赖 A 时不会报错，顺序退化为发现顺序。

三阶段执行顺序：

| 阶段 | 顺序 | 用途 |
| --- | --- | --- |
| `PreConfigureServices` | 依赖在前（正序） | 在依赖方注册前做前置准备 |
| `ConfigureServices` | 正序 | 主体注册 |
| `PostConfigureServices` | 逆序 | 覆盖/收尾，后注册的模块先执行 |

每个模块实例用 `Activator.CreateInstance` 创建后以 `Singleton(moduleType, instance)` 注册，因此后续阶段拿到的是**同一个实例**。

### 3.3 生命周期钩子

`OnApplicationInitializationAsync` / `OnApplicationShutdownAsync` 通过 `ModuleInitializationHostedService` 接入通用主机：

```csharp
// AddFrameService 内部（有模块时）
services.AddSingleton<IModuleInitializer>(sp => new ModuleInitializer(sortedModuleTypes));
services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ModuleInitializationHostedService>());
```

- `StartAsync`：按排序正序调用每个模块的 `OnApplicationInitializationAsync`；
- `StopAsync`：**逆序**调用 `OnApplicationShutdownAsync`；
- 日志用 `ILogger<ModuleInitializationHostedService>`（可选注入，测试时可传 null）。

> 前提是宿主执行 `IHostedService`。ASP.NET Core 的 `WebApplication` / `Host` 默认会执行；自己 `new ServiceCollection().BuildServiceProvider()` 的裸容器不会。

### 3.4 框架注册的基础设施

`RegisterInfrastructure` 用 `TryAdd*`，所以业务可以先注册自己的实现覆盖：

| 服务 | 实现 | 生命周期 |
| --- | --- | --- |
| `IUnitOfWorkManager` | `UnitOfWorkManager` | Scoped |
| `IClock` | `SystemClock` | Singleton |
| `ICurrentUser` | `NullCurrentUser` | Scoped |
| `IAuditPropertySetter` | `AuditPropertySetter` | Scoped |
| `IModuleInitializer` | `ModuleInitializer`（仅存在模块时） | Singleton |

## 4. 写一个新模块

```csharp
[DependsOn(typeof(DatabaseModule))]
public sealed class OrderingModule : FrameModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Add(ServiceDescriptor.Scoped<IOrderRepository, OrderRepository>());
        context.Add(ServiceDescriptor.Scoped<IOrderService, OrderService>());
    }

    public override async Task OnApplicationInitializationAsync(CancellationToken cancellationToken = default)
    {
        // 预热缓存、校验配置……
        await Task.CompletedTask;
    }
}
```

宿主侧无需显式注册模块：只要模块类型在入口程序集的可达引用里，`AddFrameService` 就会扫描到。若模块在未被引用的程序集里，用 `options.AddAssemblyOf<OrderingModule>()` 补上。

模块内注册服务时优先用 `ServiceConfigurationContext.Add`，不要直接操作 `IServiceCollection`——后者会绕过模块顺序与后续的 `RemoveService` 覆盖能力。

## 5. 项目集分层（示例）

模块系统解决「一个程序集内怎么装配」，多宿主项目还需要解决「多个宿主怎么共用同一套装配」。`samples/TaskBoard` 给出的分层是：

```
Domain           聚合与不变量，无框架依赖（只引 Frame.Core）
Application      用例编排，接口上标 [UnitOfWork]
Infrastructure   持久化实现 + 组合根 AddTaskBoard(...)
Web              宿主：HTTP 端点、过滤器、当前用户
Worker           宿主：AddTaskBoard(config, o => o.UseScheduler())
Tests            领域层单元测试，只引 Domain
```

`AddTaskBoard` 是唯一装配入口，Web 与 Worker 都调用它，只在 `configure` 回调里追加自己的能力（例如 `UseScheduler()`）。完整的目录树、端点和建表 SQL 见 [示例工程 · TaskBoard](samples.md#7-项目集方案-taskboard)。

## 6. 边界与约束

- `src/*` 之间只允许依赖 `Frame.Core`，不允许 `Frame.Databases` 引用 `Frame.EventBus` 这类横向依赖；
- `samples/*` 与 `Test/*` 不被 `src/*` 引用；
- 模块的 `ConfigureServices` 只注册自己的服务，不假设宿主是 ASP.NET Core；
- 新增基础设施服务用 `TryAdd*`，保证可替换；
- 模块生命周期钩子里不要做长耗时阻塞操作——它在宿主启动/停止路径上。

相关文档：[整体架构](architecture.md) · [核心模块](core.md) · [工作单元与自动事务](unit-of-work.md)
