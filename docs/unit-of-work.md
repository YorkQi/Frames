# 工作单元与自动事务

工作单元（Unit of Work，UOW）负责把一次业务操作中的多次数据库写入合并成一个提交点。Frames 提供两种用法：

| 用法 | 入口 | 是否自动提交/回滚 |
| --- | --- | --- |
| 显式 | `IUnitOfWorkManager.Begin()` / `ApplicationService.BeginUnitOfWork()` | 需要自己 `CompleteAsync()` / `RollbackAsync()` |
| 声明式 | 在应用服务类或方法上标 `[UnitOfWork]` | 由动态代理自动提交/回滚 |

相关源码：

| 文件 | 内容 |
| --- | --- |
| `src/Frame.Core/Uow/IUnitOfWork.cs` | UOW 契约：`Items`、完成/回滚处理器、`CompleteAsync`/`RollbackAsync` |
| `src/Frame.Core/Uow/UnitOfWork.cs` | 嵌套、子 UOW、处理器执行顺序、超时 |
| `src/Frame.Core/Uow/UnitOfWorkManager.cs` | `AsyncLocal` 当前 UOW、最外层/嵌套判定 |
| `src/Frame.Core/Uow/UnitOfWorkAttribute.cs` | `[UnitOfWork]` 特性 |
| `src/Frame.Core/Uow/Interception/UnitOfWorkProxy.cs` | `DispatchProxy` 拦截器 |
| `src/Frame.Core/Application/ServiceApplicationConfiguration.cs` | 扫描应用服务并决定是否套代理 |
| `src/Frame.Databases/DatabaseContext.cs` | 把 `IDbContext` 挂到 UOW，提交时落库并分发事件 |

## 1. 声明式用法

```csharp
public interface IOrderService : IApplicationService
{
    Task<int> PlaceAsync(Order order, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly IRepository<int, Order> _orders;

    public OrderService(IRepository<int, Order> orders) => _orders = orders;

    [UnitOfWork]   // 也可以标在类上，作用于该类的所有方法
    public async Task<int> PlaceAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _orders.InsertAsync(order, cancellationToken);
        return order.Id;
    }
}
```

调用方只注入接口：

```csharp
public class OrderController : ControllerBase
{
    private readonly IOrderService _service;
    public OrderController(IOrderService service) => _service = service;

    [HttpPost]
    public Task<int> Place(Order order) => _service.PlaceAsync(order);
}
```

`PlaceAsync` 返回时，UOW 已经提交：变更被 `SaveChangesAsync` 落库、事务提交、领域事件分发完毕。中途抛异常则回滚并原样抛出。

## 2. 它是怎么接上的

`UseApplication()` 扫描程序集时，对每个实现了 `IApplicationService` 派生接口的具体类做判断：

```csharp
// 类级、实现类方法级、接口级、接口方法级，任一命中即代理
private static bool RequiresUnitOfWork(Type type, Type serviceInterface) =>
    type.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true) is not null ||
    serviceInterface.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true) is not null ||
    type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Any(m => m.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true) is not null) ||
    serviceInterface.GetMethods()
        .Any(m => m.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true) is not null);
```

因此 `[UnitOfWork]` 既可以标在实现类/实现方法上，也可以标在接口或接口方法上（推荐后者，声明式契约更直观）。

命中时注册的工厂不是 `Scoped(interface, type)`，而是：

```csharp
configuration.Add(ServiceDescriptor.Scoped(serviceInterface, sp =>
    UnitOfWorkProxy.Create(
        serviceInterface,
        ActivatorUtilities.CreateInstance(sp, type),
        sp.GetRequiredService<IUnitOfWorkManager>())));
```

`UnitOfWorkProxy.Create` 用 `DispatchProxy.Create<T, UnitOfWorkInterceptor>()` 生成实现该接口的代理，`UnitOfWorkInterceptor.Invoke` 的逻辑：

1. 先看接口方法上的 `[UnitOfWork]`，再看实现类同名方法上的，最后看目标类上的；都没有 → 直接调用真实方法；
2. 有特性 → `_manager.Begin(new UnitOfWorkOptions { IsAutoRollback = ..., Timeout = ... })`；
3. 调用真实方法；
4. 按返回类型包装：`Task` / `Task<T>` / `ValueTask` / `ValueTask<T>` 在 await 完成后提交；同步返回值立即提交；
5. 任何异常 → 回滚（async 路径先回滚再抛，同步路径立即回滚再抛）。

## 3. 返回值与异常语义

| 方法签名 | 提交时机 | 异常行为 |
| --- | --- | --- |
| `Task` / `Task<T>` | `await` 完成后、调用方拿到结果前 | 目标方法异常或提交异常都会回滚 |
| `ValueTask` / `ValueTask<T>` | 同上 | 同上 |
| 同步返回值 `T` | 方法返回前（阻塞等待提交） | 目标方法异常会回滚；**提交阶段的异常直接抛出，不回滚** |
| `void` | 方法返回前 | 同上 |

注意 `void` 与同步方法：`CompleteAsync().GetAwaiter().GetResult()` 会同步阻塞。在 ASP.NET Core 请求线程上问题不大，但不要在 UI 线程或已有同步上下文里大量使用。

## 4. 与数据库的集成

UOW 本身只管「什么时候提交」，真正落库由 `Frame.Databases` 注册的完成处理器完成。

`DatabaseContext.GetRepository<T>()` 第一次被调用时会执行 `AttachToUnitOfWork()`：

```csharp
unitOfWork.Items[DbContextItemKey] = dbContext;                 // "Frame.Databases.IDbContext"
unitOfWork.RegisterCompletionHandler(ct => FlushAndDispatchAsync(dbContext, ct));
unitOfWork.RegisterRollbackHandler(() => dbContext.Rollback());
```

`FlushAndDispatchAsync` 的顺序：

1. `GetTrackedEntities()` 取所有被追踪实体（含 `Unchanged`）上挂起的领域事件，`GetChangedEntities()` 判断是否需要落库；
2. 在 `SaveChanges` **之前**取走这些实体的 `IHasDomainEvents.DomainEvents`（保存后状态变为 `Unchanged`，事件仍在，但这里提前取更稳妥）；
3. 有变更 → `BeginTransactionAsync` → `ApplyAuditing` 由 `SaveChangesAsync` 内部执行 → 提交；
4. 提交成功后，逐个调用 `IDomainEventDispatcher.DispatchAsync`（默认由 `Frame.EventBus` 提供实现）；
5. 清空事件集合。

因此：

- **只有本次 UOW 内至少用过一次 `DatabaseContext.GetRepository`，才会自动落库**；纯内存操作不会开启事务；
- 没有变更、也没有事件时处理器直接返回，不产生事务开销；
- 提交与分发之间没有 outbox：进程在「已提交、未分发完」之间崩溃，事件会丢（见 [design-notes.md](design-notes.md#9-事件总线的可靠性边界)）。

## 5. `Items` 与处理器

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    string Id { get; }
    bool IsCompleted { get; }
    bool IsRolledBack { get; }
    IDictionary<string, object?> Items { get; }
    void RegisterCompletionHandler(Func<CancellationToken, Task> handler);
    void RegisterRollbackHandler(Action handler);
    Task CompleteAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

- `Items` 由**最外层** UOW 持有，嵌套 UOW 通过 `Root` 共享同一字典，所以嵌套里写入的键外层可见；
- `RegisterCompletionHandler` / `RegisterRollbackHandler` 也一律提升到最外层，避免「子 UOW 完成时只标记、处理器丢失」；
- 完成处理器按注册顺序执行，**在** `OnCompleteCallback` 之前、所有子 UOW 完成之后；
- 回滚处理器**逆序**执行，用于释放后申请的资源。

```csharp
var uow = manager.Begin();
uow.Items["trace-id"] = Activity.Current?.Id;
uow.RegisterCompletionHandler(ct => auditLog.WriteAsync("committed", ct));
uow.RegisterRollbackHandler(() => tempFiles.Cleanup());
```

## 6. 嵌套与超时

- `Begin()` 在已有 `Current` 时创建子 UOW，子 UOW 完成只置 `_isCompleted`，不触发提交；
- 最外层 `CompleteAsync` 会先完成所有 children，再执行处理器与 `OnCompleteCallback`；
- 最外层提交/回滚后，`UnitOfWorkManager.ClearCurrent()` 清理当前上下文；
- `UnitOfWorkOptions.Timeout` 会创建 `CancellationTokenSource`，超时触发 `RollbackAsync`。`[UnitOfWork(TimeoutSeconds = 5)]` 即 5 秒超时。

> `UnitOfWorkManager` 用 `AsyncLocal<Holder>` 而不是 `AsyncLocal<UnitOfWork>`：在 async 方法内给 `AsyncLocal.Value` 赋 `null` 不会回传给调用方，只有修改共享 holder 对象才能让「提交后 `Current` 变 null」对调用方生效。这是踩过的坑，改这块时不要退回去。

## 7. 限制与陷阱

- **只代理接口**：`DispatchProxy` 只能为接口生成代理。应用服务必须通过 `IApplicationService` 派生接口注入；直接 `GetRequiredService<OrderService>()` 拿到的是原始类，没有事务。
- **一个类只注册第一个接口**：`UseApplication` 取 `GetInterfaces().FirstOrDefault(...)`，实现多个应用服务接口时其余接口解析失败。把接口拆到不同类，或手工注册。
- **类级特性影响所有方法**：包括查询方法，会给只读操作也开 UOW。只读服务别标类级特性。
- **代理方法内不能 `new` 目标类**：代理包裹的是容器创建的实例；在方法里 `new OrderService()` 不经过代理。
- **`Dispose` 不被拦截**：代理上的 `Dispose()` 直接转发，不会触发提交/回滚；UOW 由方法边界管理。
- **同步方法的提交异常不回滚**（见上表），需要强一致时用 async 方法。
- **没有 `[UnitOfWork]` 的显式用法仍然有效**：`manager.Begin()` / `ApplicationService.BeginUnitOfWork()` 不会被代理重复包裹（代理只处理带特性的方法）。
- **UOW 内的 `DatabaseContext` 是同一个**：`GetRepository` 复用同一 `IServiceScope` 与 `IDbContext`，不要在一个 UOW 里手动 `new` 另一个 `DatabaseContext`。

## 8. 排查清单

| 现象 | 检查 |
| --- | --- |
| 数据没落库 | 方法是否通过接口调用？类/方法是否有 `[UnitOfWork]`？是否用过 `GetRepository`？ |
| 每次调用都开新事务，嵌套没生效 | 是否在 `Task.Run` / 新线程里调用？`AsyncLocal` 不跨线程自动流动 |
| `Current` 提交后仍非空 | 是否绕过了 `UnitOfWorkManager`（例如自己 `new UnitOfWork`）？ |
| 提交后事件没发 | 是否调用 `UseEventBus()` 注册了 `IDomainEventDispatcher`？实体是否有 EF 变更？ |
| 回滚后数据库仍有数据 | 是否在 UOW 外直接调用了 `SaveChangesAsync`？ |

相关文档：[核心模块](core.md) · [数据库访问](databases.md) · [领域事件](domain-events.md) · [审计字段](auditing.md)
