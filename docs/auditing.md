# 审计字段

审计字段用于记录「谁在什么时候创建/修改/删除了数据」。Frames 把这套能力放在 `Frame.Core.Auditing`，由 EF Core 通道在 `SaveChanges` 前自动填充。

## 1. 接口

| 接口 | 成员 | 含义 |
| --- | --- | --- |
| `IHasCreationTime` | `DateTime CreationTime { get; set; }` | 创建时间（UTC） |
| `ICreationAuditedObject : IHasCreationTime` | `string? CreatorId { get; set; }` | 创建人 |
| `IHasModificationTime` | `DateTime? LastModificationTime { get; set; }` | 最后修改时间 |
| `IModificationAuditedObject : IHasModificationTime` | `string? LastModifierId { get; set; }` | 最后修改人 |
| `ISoftDelete` | `bool IsDeleted { get; set; }` | 软删除标记 |
| `IHasDeletionTime` | `DateTime? DeletionTime { get; set; }` | 删除时间 |
| `IDeletionAuditedObject : IHasDeletionTime` | `string? DeleterId { get; set; }` | 删除人 |
| `IAuditedObject : ICreationAuditedObject, IModificationAuditedObject` | — | 创建 + 修改 |
| `IFullAuditedObject : IAuditedObject, IDeletionAuditedObject, ISoftDelete` | — | 全套 |

实体按需实现，不必全部实现：

```csharp
public class User : Entity<int>, IFullAuditedObject
{
    public string Name { get; set; } = null!;
    public DateTime CreationTime { get; set; }
    public string? CreatorId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public string? LastModifierId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
    public string? DeleterId { get; set; }
}
```

> 时间统一用 UTC（`IClock.Now` 返回 `DateTime.UtcNow`）。展示层自行转本地时区。

## 2. 谁提供时间与用户

| 抽象 | 默认实现 | 生命周期 | 替换方式 |
| --- | --- | --- | --- |
| `IClock` | `SystemClock`（`DateTime.UtcNow`） | Singleton | `services.AddSingleton<IClock, MyClock>()` |
| `ICurrentUser` | `NullCurrentUser`（`Id`/`Name` 均为 null） | Scoped | `services.AddScoped<ICurrentUser, HttpCurrentUser>()` |
| `IAuditPropertySetter` | `AuditPropertySetter` | Scoped | 一般不需要替换 |

三者都由 `AddFrameService` 用 `TryAdd*` 注册，因此**先注册自己的实现即可覆盖**（在 `AddFrameService` 的 configuration 回调里注册，会排在框架默认注册之前）。典型做法：

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? Id => accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Name => accessor.HttpContext?.User.Identity?.Name;
}
```

## 3. 填充规则

`AuditPropertySetter` 的三个方法：

| 方法 | 写入 |
| --- | --- |
| `SetCreationProperties(entity)` | `IHasCreationTime.CreationTime = clock.Now`；`ICreationAuditedObject.CreatorId = currentUser.Id` |
| `SetModificationProperties(entity)` | `IHasModificationTime.LastModificationTime = clock.Now`；`IModificationAuditedObject.LastModifierId = currentUser.Id` |
| `SetDeletionProperties(entity)` | `ISoftDelete.IsDeleted = true`；`IHasDeletionTime.DeletionTime = clock.Now`；`IDeletionAuditedObject.DeleterId = currentUser.Id` |

实体没实现对应接口时自动跳过；传入 `null` 抛 `ArgumentNullException`。

> 删除只写删除相关字段，**不会**同时更新 `LastModificationTime`。需要的话在业务层显式赋值，或替换 `IAuditPropertySetter`。

## 4. EF Core 通道的自动填充

`DbContext.SaveChanges` / `SaveChangesAsync` 在真正写库前调用 `ApplyAuditing()`：

| `EntityState` | 动作 |
| --- | --- |
| `Added` | `SetCreationProperties` |
| `Modified` | `SetModificationProperties` |
| `Deleted` 且实体实现 `ISoftDelete` | `SetDeletionProperties`，并把状态改为 `Modified`（不产生 `DELETE`） |
| `Deleted` 且未实现 `ISoftDelete` | 保持 `Deleted`，物理删除 |

因为发生在 `SaveChanges` 内部，**无论你是手工调用还是由 UOW 自动提交，审计都会生效**。`Insert` / `Update` / `Delete` 本身只改追踪状态，此时审计字段还没写；检查实体时看到的是旧值，属于正常现象。

`ApplyAuditing` 通过 `ChangeTracker.Entries()` 遍历，所以只对 EF Core 跟踪的实体生效。

## 5. 软删除的后果

- **不会自动过滤查询**：框架没有注册全局查询筛选器。`GetAllEntity` 走 Dapper `SELECT *`，会查出已删除数据；EF 的 `Find` / `Queryable` 也不会排除。需要时自行加 `WHERE IsDeleted = 0` 或配置全局筛选器。
- **软删除后唯一索引仍占用**：例如 `Name` 唯一索引会与已删除记录冲突。常用做法是改用 `(Name, IsDeleted)` 复合索引，或删除时改写字段。
- **`DeleteBatch` 也走延迟持久化**：改为逐个 `Remove`，由 `SaveChanges` 统一转成软删除。

## 6. Dapper 通道不参与

`DbParameters` / `QueryAsync` / `ExecuteAsync` 直接拼接并执行 SQL，框架**不做任何审计注入**。如果业务用 Dapper 写数据，需要自己设置审计字段（或统一走 EF Core 仓储）。

## 7. 测试

`Test/TestUnit/AuditPropertySetterTests.cs` 覆盖：

- 创建时写入 `CreationTime` / `CreatorId`；
- 修改时写入 `LastModificationTime` / `LastModifierId`；
- 删除时写入 `IsDeleted` / `DeletionTime` / `DeleterId`；
- 实体未实现接口时静默跳过；
- `null` 参数抛 `ArgumentNullException`；
- 时间来源可替换（固定 `IClock` 断言精确值）。

EF Core 的 `ApplyAuditing` 需要真实 `DbContext`，当前没有集成测试；改动这段逻辑时建议补一个 SQLite/内存库的测试。

相关文档：[工作单元与自动事务](unit-of-work.md) · [数据库访问](databases.md) · [核心模块](core.md)
