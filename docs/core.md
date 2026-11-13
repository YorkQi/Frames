# 核心模块 Core

`Frame.Core` 提供六组契约：实体、仓储、工作单元、应用服务、模块系统、分页 DTO。它不引用任何数据库/缓存实现，可以被领域层单独引用。

命名空间一览：

| 命名空间 | 内容 |
| --- | --- |
| `Frame.Core` | `AddFrameService`、`ServiceConfigurationContext` |
| `Frame.Core.Entities` | `IEntity`、`IEntity<TKey>`、`Entity<TKey>` |
| `Frame.Core.Entities.Dtos` | `PageInput`、`PageResult<T>` |
| `Frame.Core.Repositories` | `IRepository`、`IRepository<TKey,TEntity>` |
| `Frame.Core.Uow` | `IUnitOfWork`、`IUnitOfWorkManager`、`UnitOfWorkOptions`、`UnitOfWorkAttribute` |
| `Frame.Core.Application` | `IApplicationService`、`ApplicationService` |
| `Frame.Core.Modules` | `IFrameModule`、`FrameModule`、`DependsOnAttribute`、`ServiceModuleAttribute` |

## 1. 实体

```csharp
public interface IEntity { }

public interface IEntity<TPrimaryKey> : IEntity
{
    TPrimaryKey Id { get; set; }
}

public abstract class Entity<TPrimaryKey> : IEntity<TPrimaryKey>
    where TPrimaryKey : IEquatable<TPrimaryKey>
{
    public TPrimaryKey Id { get; set; } = default!;
    public IReadOnlyList<object>? DomainEvents { get; }
    protected void AddDomainEvent(object eventData);
    protected void RemoveDomainEvent(object eventData);
    protected void ClearDomainEvents();
    public virtual bool IsTransient();
}
```

### 1.1 相等性规则

`Equals` 的判断顺序：

1. `obj` 为 `null` 或不是 `Entity<TPrimaryKey>` → `false`（泛型参数必须一致，`Entity<int>` 与 `Entity<long>` 互不相等）；
2. 同一引用 → `true`；
3. 两个运行时类型互不可赋值（典型：同基类的两个兄弟实体类型）→ `false`；
4. 双方都是新实体（`Id` 为默认值）→ `false`；
5. 否则比较 `Id`。

`GetHashCode`：

- 新实体：`GetType().GetHashCode()`（避免所有新实体哈希都是 0）；
- 已持久化：`Id.GetHashCode()`。

`==` / `!=` 运算符委托给 `Equals`，支持 `null` 比较。

> 推论：两个不同 `Id` 的已持久化同类型实体不相等；两个都未赋 `Id` 的同类型实体也不相等，所以**不能把未保存的实体放进 `HashSet` 去重**。

`IsTransient()` 是 `virtual`，可用 `Id == 0` 之外的业务规则覆盖（例如 Guid 主键判断 `Guid.Empty`）。

### 1.2 领域事件（未接线）

`Entity` 提供 `AddDomainEvent` / `RemoveDomainEvent` / `ClearDomainEvents` 三个 `protected` 方法，事件对象存在私有 `List<object>` 里，通过只读属性 `DomainEvents` 暴露。

**框架内没有任何代码读取 `DomainEvents` 或分发事件**：UOW 的完成回调是空实现，也没有拦截器。目前它只是一个容器——需要分发时必须自己写代码，见 [design-notes.md](design-notes.md#2-领域事件没有分发器)。

### 1.3 审计接口（不存在）

`Entity<TKey>` 的 XML 注释提到 `IHasCreationTime`、`IModificationAuditedObject`、`ISoftDelete`、`IDeletionAuditedObject`，但**这些接口在仓库里不存在**（grep 无命中）。引用它们会导致编译错误，注释本身也会产生 `CS1574` 文档警告。需要审计字段时，自己在实体上定义属性并在 `SaveChanges` 前后赋值。

## 2. 仓储契约

```csharp
public interface IRepository { }   // 标记，用于扫描与 `GetRepository<T>()` 约束

public interface IRepository<TPrimaryKey, TEntity> : IRepository
{
    TEntity Get(TPrimaryKey id, CancellationToken cancellationToken = default);
    Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default);

    IEnumerable<TEntity> QueryAll(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> QueryAllAsync(CancellationToken cancellationToken = default);

    IEnumerable<TEntity> Query(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> QueryAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);

    int Insert(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    int InsertBatch(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<int> InsertBatchAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    int Update(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    int UpdateBatch(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<int> UpdateBatchAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    int Delete(TPrimaryKey id, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(TPrimaryKey id, CancellationToken cancellationToken = default);
    int DeleteBatch(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);
    Task<int> DeleteBatchAsync(IEnumerable<TPrimaryKey> ids, CancellationToken cancellationToken = default);
}
```

设计要点：

- **只定义契约，不定义实现**：实现在 `Frame.Databases.Repository<,>`，领域层可以只依赖接口。
- **接口对 `TEntity` 无条件约束**（实现类 `Repository<,>` 才要求 `class, IEntity<TPrimaryKey>`），因此 `IRepository<int, string>` 也能通过编译，只在解析实现时失败。
- **同步方法也接收 `CancellationToken`**，仅为签名统一；实现里不会主动检查，取消只在异步路径上生效。
- **写操作返回 `int` 但 EF Core 通道恒为 0**（延迟持久化），`DeleteBatch` 例外返回真实受影响行数。不要用返回值判断成功与否。
- `Get` 返回非可空 `TEntity`，查不到时由实现决定（EF Core 通道返回 `null`，实际是 `null!`），调用方按可空处理更安全。
- 没有 `GetPaged`、`Count`、`Any` 等方法；复杂查询由业务仓储接口自行声明（示例 `IUserRepository` 就加了三个方法）。
- `Query(ids)` 在 EF Core 通道里用 `WHERE Id IN @ids`（Dapper），不走变更追踪。

## 3. 工作单元（UOW）

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    string Id { get; }
    bool IsCompleted { get; }
    bool IsRolledBack { get; }
    Task CompleteAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkManager
{
    IUnitOfWork Begin(UnitOfWorkOptions? options = null);
    IUnitOfWork? Current { get; }
}

public class UnitOfWorkOptions
{
    public bool IsAutoRollback { get; set; }
    public TimeSpan? Timeout { get; set; }
}
```

### 3.1 嵌套语义

`UnitOfWorkManager.Begin` 的实现：

- 有外层 UOW 时，新 UOW 的 `_outer` 指向它，并**继承外层的 `OnCompleteCallback` / `OnRollbackCallback`**；
- 只有最外层 UOW 会写入 `AsyncLocal`（`Current` 始终指向最外层）；
- 子 UOW 被加入外层的 `_children` 列表。

`CompleteAsync`：

| 场景 | 行为 |
| --- | --- |
| 已完成 | 直接返回 |
| 已回滚 | 抛 `InvalidOperationException("工作单元 {Id} 已回滚，无法提交。")` |
| 嵌套且外层未完成 | 只置 `_isCompleted = true`，**不执行回调、不提交** |
| 最外层 | 依次 `CompleteAsync` 所有 children → 执行 `OnCompleteCallback` → 置完成 → 释放超时 CTS → `UnitOfWorkManager.ClearCurrent()` |

`RollbackAsync`：已回滚/已完成直接返回；有外层且外层未回滚时**冒泡到外层**执行回滚；否则执行 `OnRollbackCallback` 并清理 `Current`。

`Dispose`：未完成且未回滚时，**无论 `IsAutoRollback` 取值都回滚**（当前两条分支代码相同），然后把自己从外层的 children 移除。

> 这意味着「用 `using` 包住 UOW、正常结束不显式 Complete」会回滚。必须显式 `await uow.CompleteAsync()`。

### 3.2 回调注册（空实现）

`Frame.Databases` 在 `UseDatabaseContext` 里调用：

```csharp
UnitOfWorkManager.RegisterDbActions(onComplete: async ct => { await Task.CompletedTask; });
```

这是**静态**注册，写入 `UnitOfWorkManager._defaultOnComplete`，本意是「UOW 完成时统一 `SaveChangesAsync`」，但回调体是空的，且只有最外层 UOW 触发。因此：

- UOW 提交**不会**把 EF Core 的挂起变更写入数据库；
- 必须自己在业务代码里调用 `IDbContext.SaveChangesAsync()`。

### 3.3 `[UnitOfWork]`（未接线）

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class UnitOfWorkAttribute : Attribute
{
    public bool IsAutoRollback { get; set; }
    public int TimeoutSeconds { get; set; }
    public TimeSpan? Timeout { get; }
}
```

框架没有动态代理/拦截器，`UnitOfWorkAttribute` **不会被任何代码读取**。标在方法上等于注释。要使用 UOW 只能显式写代码（见下节）。

### 3.4 正确用法

```csharp
public class OrderService : ApplicationService
{
    private readonly IDbContext _db;

    public OrderService(IUnitOfWorkManager uowManager, IDbContext db) : base(uowManager)
        => _db = db;

    public async Task PlaceOrderAsync(Order order)
    {
        var uow = BeginUnitOfWork(new UnitOfWorkOptions { IsAutoRollback = true });
        try
        {
            // ... 领域操作、仓储写入
            await _db.SaveChangesAsync();          // 必须显式提交数据库变更
            await CompleteUnitOfWorkAsync();       // 触发回调（当前为空）
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }
}
```

`ApplicationService.CompleteUnitOfWorkAsync` 只在 `Current != null && !Current.IsCompleted` 时提交，所以嵌套调用是安全的（子 UOW 里调用只会标记完成，最外层才真正触发回调）。

> ⚠️ `ApplicationService` 的无参构造函数把 `UnitOfWorkManager` 置为 `null!`。若类继承了 `ApplicationService` 却用无参构造，调用 `BeginUnitOfWork` 会 `NullReferenceException`。

## 4. 应用服务

```csharp
public interface IApplicationService { }

public abstract class ApplicationService : IApplicationService
{
    protected IUnitOfWorkManager UnitOfWorkManager { get; }
    protected ApplicationService();
    protected ApplicationService(IUnitOfWorkManager unitOfWorkManager);
    protected IUnitOfWork BeginUnitOfWork(UnitOfWorkOptions? options = null);
    protected Task CompleteUnitOfWorkAsync(CancellationToken cancellationToken = default);
}
```

`UseApplication()` 的注册规则：

```
对每个 public class:
    取 type.GetInterfaces().FirstOrDefault(i =>
        i != typeof(IApplicationService) && typeof(IApplicationService).IsAssignableFrom(i))
    若存在 → services.AddScoped(interface, type)
```

- 不继承 `IApplicationService` 的类不会被注册；
- 一个类实现多个 `IApplicationService` 派生接口时，**只有第一个（反射返回顺序）被注册**，其余接口解析会失败；
- 不注册自身类型（`UserService` 本身解析不到），只能按接口注入。

## 5. 分页 DTO

```csharp
public class PageInput
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
}

public class PageResult<TEntity>
{
    public PageResult(IEnumerable<TEntity> items, long count);
    public IEnumerable<TEntity> Items { get; }
    public long Count { get; }
}
```

`PageInput` 只是请求参数载体，不做校验；`PageResult<T>` 由 `IDbContext.QueryPagedAsync` 构造，`Count` 是总记录数（不是当前页条数）。分页 SQL 的生成见 [databases.md](databases.md#8-分页与方言)。

## 6. 模块系统

模块系统的完整说明见 [architecture.md](architecture.md#6-模块系统)，这里只列契约：

```csharp
public interface IFrameModule
{
    void PreConfigureServices(ServiceConfigurationContext context);
    void ConfigureServices(ServiceConfigurationContext context);
    void PostConfigureServices(ServiceConfigurationContext context);
    Task OnApplicationInitializationAsync(CancellationToken cancellationToken = default);
    Task OnApplicationShutdownAsync(CancellationToken cancellationToken = default);
}

public abstract class FrameModule : IFrameModule { /* 5 个空实现 */ }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DependsOnAttribute : Attribute
{
    public DependsOnAttribute(params Type[] dependedModuleTypes);
    public Type[] DependedModuleTypes { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ServiceModuleAttribute : Attribute
{
    public ServiceModuleAttribute(ServiceLifetime lifetime);
    public ServiceLifetime Lifetime { get; }
}
```

`ServiceConfigurationContext` 是装配期的可变集合，公开成员：

| 成员 | 用途 |
| --- | --- |
| `AddAssembly(Assembly)` / `AddAssemblyOf<T>()` | 追加扫描程序集 |
| `GetAssemblyTypes()` | 取所有已发现程序集的 public 类型 |
| `GetType(string fullName)` | 按完整名找类型（供 `JobSchedule.FullName` 使用） |
| `Add(ServiceDescriptor)` / `AddRange(...)` | 追加注册 |
| `RemoveService(Func<ServiceDescriptor,bool>)` | 移除注册（可在模块里覆盖默认实现） |
