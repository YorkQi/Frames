# 数据库访问

`Frame.Databases`（`net8.0`）把 EF Core 的变更追踪和 Dapper 的手写 SQL 合并到同一个 `IDbContext` 上，再由 `DatabaseContext` 管理「一次请求内共享一个上下文」的作用域。

```
DatabaseContext（Scoped，业务注入这个）
   └─ 懒建 IServiceScope
        ├─ DbContextConnectionStringAccessor（Scoped，本次请求选中的连接串）
        └─ IDbContext（Scoped）
             └─ DbContext（partial class，分两个文件）
                  ├─ EfCoreDbContext.cs   ← EF Core：实体 CRUD、SaveChanges、事务
                  └─ DapperDbContext.cs   ← Dapper：SQL 查询/执行、分页、批量删除
```

## 1. 注册

```csharp
options.UseDatabaseContext<CommandDatabaseContext>(
    new ConnectionStringCollection("Server=...;Database=app;..."),
    ConnectionStringStrategy.RoundRobin);
```

签名：

```csharp
public static ServiceConfigurationContext UseDatabaseContext<TDatabaseContext>(
    this ServiceConfigurationContext context,
    ConnectionStringCollection connectionStrings,
    ConnectionStringStrategy strategy = ConnectionStringStrategy.Random)
    where TDatabaseContext : DatabaseContext, new();
```

内部按顺序做四件事：

1. `configuration.Add(Scoped<DbContextConnectionStringAccessor>)`（保存本次请求的连接串）；
2. `configuration.Add(Scoped<IDbContext>(sp => { var ctx = new DbContext(accessor); ctx.Dialect = ctx.CreateDialect(); return ctx; }))`；
3. `configuration.Add(Scoped<TDatabaseContext>(sp => { var c = new TDatabaseContext(); c.Initialize(sp, collection, strategy); return c; }))`；
4. `UseRepositories()` 扫描并注册仓储；再调用 `UnitOfWorkManager.RegisterDbActions(...)`（空回调，见 [core.md](core.md#32-回调注册空实现)）。

注意这里用的是 `Add` 而不是 `TryAdd`：多次调用 `UseDatabaseContext` 会产生**多条 `IDbContext` 注册**，直接 `GetRequiredService<IDbContext>()` 拿到的是最后一条。这不会破坏读写分离，因为每个 `DatabaseContext` 子类在自己的子 Scope 里先设置 `DbContextConnectionStringAccessor` 再解析 `IDbContext`，各自拿到独立实例与连接串。

> 反过来说：**在 `DatabaseContext` 作用域之外直接注入 `IDbContext` 会失败**——父 Scope 的 `DbContextConnectionStringAccessor.ConnectionString` 从未被赋值，构造函数抛 `InvalidOperationException("连接字符串未设置。请先设置 ConnectionStringAccessor。")`。要落库请通过仓储内部的 `DBContext`（第 4 节）。

## 2. `DatabaseContext`

```csharp
public class DatabaseContext : IDisposable, IAsyncDisposable
{
    public TRepository GetRepository<TRepository>() where TRepository : IRepository;
    public IRepository<TPrimaryKey, TEntity> GetRepository<TPrimaryKey, TEntity>()
        where TEntity : class, IEntity;
}
```

行为：

- **首次**调用 `GetRepository` 时创建子 Scope：读取策略选中的连接串 → 写入 `DbContextConnectionStringAccessor` → 解析 `IDbContext` → `DbContextScope.Set(_dbContext)`；
- 之后同一 `DatabaseContext` 实例的调用复用该 Scope；
- `Dispose` 释放 Scope 并 `DbContextScope.Clear()`；
- 同一请求内不同 `DatabaseContext`（如 Query/Command）各有独立 Scope 与独立 `IDbContext`。

> `DatabaseContext` **没有** `GetDbContext()` / `SaveChanges()` 之类的转发方法，也没有公开属性暴露内部 `IDbContext`。要执行 `SaveChanges` / 事务，唯一现成通道是仓储的 `protected IDbContext DBContext`（在业务仓储里加转发方法）。直接注入 `IDbContext` 会因为父 Scope 的连接串访问器为空而抛「连接字符串未设置」。这是当前设计的一个明显缺口，见 [design-notes.md](design-notes.md#5-databasecontext-没有暴露-idbcontext)。

示例工程的做法：仓储继承 `Repository<,>` 并通过 `DbContextScope.Current`（`AsyncLocal`）拿到 `DatabaseContext` 子 Scope 里的 `IDbContext`，所以仓储上的落库操作作用在正确的实例上。

## 3. 连接串与负载策略

```csharp
public class ConnectionStringCollection : IEnumerable<string>
{
    public ConnectionStringCollection();
    public ConnectionStringCollection(params string[] connectionStrings);
    public void Add(params string[] connectionStrings);
    public void AddRange(IEnumerable<string> connectionStrings);
    public string GetConnection(ConnectionStringStrategy strategy);
    public string[] ToArray();
}

public enum ConnectionStringStrategy { Random, RoundRobin }
```

- 空集合调用 `GetConnection` 抛 `InvalidOperationException("没有可用的连接字符串")`；
- `Random`（默认）用 `Random.Shared.Next(Count)`；
- `RoundRobin` 用 `Interlocked.Increment(ref _roundRobinIndex) % Count`，从 -1 开始，因此第一次是第 0 个；
- 每次 `GetRepository` 首次建 Scope 时选一次，同一 `DatabaseContext` 生命周期内不变。

> `ConnectionStringStrategy` 在 `Frame.Databases.Enums` 与 `Frame.Redis.Enums` 各定义了一份同名枚举，使用时要写全命名空间或 `using` 别名。

## 4. 延迟持久化

这是使用本框架最容易踩的坑。

| 方法 | 实际行为 | 返回值 |
| --- | --- | --- |
| `Insert` / `InsertAsync` | `_dbContext.Add(entity)` | `0` |
| `InsertBatch` / `InsertBatchAsync` | `_dbContext.AddRange(entities)` | `0` |
| `Update` / `UpdateAsync` | `_dbContext.Update(entity)` | `0` |
| `UpdateBatch` / `UpdateBatchAsync` | `_dbContext.UpdateRange(entities)` | `0` |
| `Delete` / `DeleteAsync` | `_dbContext.Remove(entity)`（先 `Find` 再移除） | `0` |
| `DeleteBatch` / `DeleteBatchAsync` | **直接 Dapper** `DELETE FROM {table} WHERE Id IN @ids` | 真实行数 |
| `SaveChanges` / `SaveChangesAsync` | `_dbContext.SaveChanges()` | 真实行数 |

因此，下面这段**不会落库**（`Repository<,>` 上没有 `SaveChangesAsync`）：

```csharp
var repo = db.GetRepository<IUserRepository>();
await repo.InsertAsync(user);      // 只是挂到 ChangeTracker，返回 0
```

`Repository<,>` 没有暴露 `SaveChangesAsync`，但它的 `protected IDbContext DBContext` 可用，业务仓储可以自行加一个转发：

```csharp
public class UserRepository : Repository<int, User>, IUserRepository
{
    public Task<int> SaveAsync(CancellationToken ct = default) => DBContext.SaveChangesAsync(ct);
}
```

或者应用服务通过仓储暴露的方法落库（同上）。**不要**指望直接注入 `IDbContext`：父 Scope 里它的构造依赖未初始化的连接串访问器，会在解析时抛异常（第 1 节）。

### 事务

`IDbContext` 暴露 `BeginTransaction(IsolationLevel)` / `BeginTransactionAsync(...)` / `Commit` / `CommitAsync` / `Rollback` / `RollbackAsync`。EF Core 实现把事务保存在 `_efTransaction` 字段，Dapper 方法通过 `GetDbTransaction()` 自动挂到当前事务。

事务**与 UOW 无关**：UOW 不会开启事务，也不会提交事务。要「要么全成功」必须自己 `BeginTransaction` + `Commit`。

## 5. `IDbContext` 能力清单

### 5.1 连接与事务

```csharp
DbConnection GetDbConnection(CancellationToken ct = default);
Task<DbConnection> GetDbConnectionAsync(CancellationToken ct = default);

void BeginTransaction(IsolationLevel level = IsolationLevel.Unspecified, CancellationToken ct = default);
Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.Unspecified, CancellationToken ct = default);
void Commit(CancellationToken ct = default);  Task CommitAsync(CancellationToken ct = default);
void Rollback(CancellationToken ct = default); Task RollbackAsync(CancellationToken ct = default);
```

事务保存在 `_efTransaction`，`Commit`/`Rollback` 后置空；重复调用是空操作。没有「嵌套事务/保存点」支持。

### 5.2 EF Core 实体操作

```csharp
// 主键查询（走 EF Core）
TEntity Get<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken ct = default);
Task<TEntity> GetAsync<TPrimaryKey, TEntity>(TPrimaryKey id, CancellationToken ct = default);

// 多主键查询（走 Dapper：SELECT * FROM 表 WHERE Id IN @ids）
IEnumerable<TEntity> GetEntity<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken ct = default);
Task<IEnumerable<TEntity>> GetEntityAsync<TPrimaryKey, TEntity>(IEnumerable<TPrimaryKey> ids, CancellationToken ct = default);

// 全表查询（走 Dapper：SELECT * FROM 表）
IEnumerable<TEntity> GetAllEntity<TPrimaryKey, TEntity>(CancellationToken ct = default);
Task<IEnumerable<TEntity>> GetAllEntityAsync<TPrimaryKey, TEntity>(CancellationToken ct = default);

// EF Core 原生能力
TEntity? Find<TEntity>(params object[] keys) where TEntity : class, IEntity;
ValueTask<TEntity?> FindAsync<TEntity>(params object[] keys) where TEntity : class, IEntity;
IQueryable<TEntity> Queryable<TEntity>() where TEntity : class, IEntity;
EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class, IEntity;
EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class, IEntity;
void Detach<TEntity>(TEntity entity) where TEntity : class, IEntity;
int SaveChanges(CancellationToken ct = default);
Task<int> SaveChangesAsync(CancellationToken ct = default);
```

- `Get`/`GetAsync` 返回 `TEntity`（非可空签名），但实现是 `(TEntity)entity!`，查不到时实际返回 `null`，调用方按可空判断。
- `GetAllEntity` / `GetEntity` **不是** EF Core 查询，而是 Dapper SQL，不受全局查询过滤器/软删除影响，也不追踪变更。
- `Queryable<TEntity>()` 适合 `Where`/`Include`/`Select`，但要求实体已注册进模型（第 7 节）。

### 5.3 Dapper SQL

```csharp
IEnumerable<T> Query<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken ct = default);
Task<IEnumerable<T>> QueryAsync<T>(...);
T QueryFirst<T>(...);  Task<T> QueryFirstAsync<T>(...);                       // 无结果抛异常
T? QueryFirstOrDefault<T>(...);  Task<T?> QueryFirstOrDefaultAsync<T>(...);
T QuerySingle<T>(...);  Task<T> QuerySingleAsync<T>(...);                     // 无结果或多行都抛异常
T? QuerySingleOrDefault<T>(...);  Task<T?> QuerySingleOrDefaultAsync<T>(...);
int Execute(string sql, ...);  Task<int> ExecuteAsync(...);
T ExecuteScalar<T>(string sql, ...);  Task<T> ExecuteScalarAsync<T>(...);
IDBContextReader QueryMultiple(string sql, ...);  Task<IDBContextReader> QueryMultipleAsync(...);
```

`param` 可以是匿名对象、`DbParameters`、或任何 Dapper 能识别的参数对象；`DbParameters` 会被转换成 `DynamicParameters`（因此支持输出参数）。所有 SQL 方法自动带上当前事务（`transaction: GetDbTransaction()`）。

### 5.4 分页

```csharp
PageResult<T> QueryPaged<T>(string sql, object? param = null, int page = 1, int size = 20,
    CommandType? commandType = null, int? commandTimeout = null, CancellationToken ct = default);
Task<PageResult<T>> QueryPagedAsync<T>(...);
```

## 6. `DbParameters`

```csharp
var p = new DbParameters()
    .Add("Name", "%张%")
    .AddIf(status.HasValue, "Status", status)
    .AddIf(!string.IsNullOrEmpty(sort), "Sort", sort, DbType.String)
    .AddOutput("Total", DbType.Int64)
    .AddReturn();

var rows = await db.ExecuteAsync("proc_QueryUsers", p);
var total = p.Get<long>("Total");        // 只能在执行后读取
```

API：

| 成员 | 说明 |
| --- | --- |
| `DbParameters()` / `DbParameters(object)` / `static From(object)` | 从对象属性生成参数（反射读 public 实例属性） |
| `Add(name, value, DbType? = null, ParameterDirection = Input, int? size = null)` | 空名字抛 `ArgumentException("参数名不能为空")` |
| `AddIf(bool condition, ...)` | 条件添加（`false` 时不影响链式） |
| `AddOutput(name, DbType, int? size = null)` | 输出参数 |
| `AddReturn(string name = "returnValue", DbType dbType = DbType.Int32)` | 存储过程返回值 |
| `Get<T>(name)` | 执行前调用抛 `InvalidOperationException("参数尚未执行，无法读取输出值。...")`；名字大小写不敏感 |
| `TryGet<T>(name, out T value)` | 同上，不抛异常 |
| `Count` / `Contains` / `Remove` / `Clear` / `Merge(other)` | 集合操作 |

每次 `Add`/`Remove`/`Clear`/`Merge` 都会把内部的 `DynamicParameters` 缓存置空，下次执行时重建——所以不要在多次执行间指望同一个 `DynamicParameters` 实例。

## 7. 实体发现

`EfCoreContext`（`Frame.Databases/EFCore/EfCoreContext.cs`）在首次使用时扫描 `AppDomain.CurrentDomain.GetAssemblies()`：

- 跳过 `System` / `Microsoft` / `netstandard` 前缀的程序集；
- 收集所有 `IEntity` 的非抽象实现；
- 在 `OnModelCreating` 中逐个 `modelBuilder.Entity(type)`；
- `ReflectionTypeLoadException` 被忽略（部分类型加载失败不影响其余）。

推论：

- 实体必须放在**已被加载**的程序集里（被业务代码引用过即可）；
- 没有 `DbSet<T>` 属性，也没有 Fluent API 配置入口；表名映射靠数据注解（`[Table]`）或约定；
- 动态加载的程序集（`Assembly.LoadFrom`、插件）如果尚未加载，实体不会被注册。

## 8. 分页与方言

```csharp
public interface IDatabaseDialect
{
    string BuildPagedSql(string baseSql, int page, int size);
    object? GetPaginationParams(int page, int size, out int offset);
    string QuoteName(string name);
    string QuoteTableName(string? schema, string tableName);
    string FormatParameter(string parameterName);
    string Name { get; }
}
```

`QueryPagedAsync` 的实现：先 `Trim()` 掉末尾分号与空白 → `Dialect.BuildPagedSql` → `QueryMultiple` 分两次读（先 `ReadSingle<long>()` 取总数，再 `Read<T>()` 取数据）。

三个方言的产出：

| 方言 | 分页 SQL |
| --- | --- |
| `MySqlDialect`（默认） | `SELECT COUNT(*) FROM ({sql}) AS __cnt;\r\n{sql} LIMIT {size} OFFSET {offset};` |
| `SqlServerDialect` | `SELECT COUNT(*) FROM ({去掉 ORDER BY 的 sql}) AS __cnt;` + `{sql} OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY;` |
| `PostgreSqlDialect` | 同 MySQL（`LIMIT/OFFSET`） |

共同约束：

- `page < 1` 抛 `ArgumentOutOfRangeException(nameof(page), "页码必须 >= 1")`；
- `size < 1` 抛 `ArgumentOutOfRangeException(nameof(size), "每页条数必须 >= 1")`；
- `GetPaginationParams` 均返回 `null`，只通过 `out offset` 给出 `(page-1)*size`；
- SQL Server 方言要求原 SQL 带 `ORDER BY`，否则 `OFFSET/FETCH` 非法。

### 方言切换

`DbContext` 的默认实现：

```csharp
public IDatabaseDialect Dialect { get; set; } = new MySqlDialect();
protected internal virtual IDatabaseDialect CreateDialect() => new MySqlDialect();
protected virtual void ConfigureProvider(DbContextOptionsBuilder builder, string connectionString)
    => builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
```

注册流程写死了 `new DbContext(accessor)`，且 `IDbContext` 用 `TryAdd`，所以：

- 想换 SQL Server/PostgreSQL，必须自己写 `DbContext` 子类重写 `ConfigureProvider` / `CreateDialect`，再**替换整个 `UseDatabaseContext` 的注册**（或先于它注册 `IDbContext`）；
- 现有 `SqlServerDialect` / `PostgreSqlDialect` 只在手工 `Dialect = new SqlServerDialect()` 时生效，没有配置入口；
- `GetTableName<TEntity>()` 的结果缓存在**静态** `ConcurrentDictionary<Type,string>` 里，缓存的是**已加引号**的表名，同一进程内切换方言会拿到旧引号风格。

## 9. 仓储

`Frame.Databases/Repositories/Repository.cs`：

```csharp
public class Repository<TPrimaryKey, TEntity> : IRepository<TPrimaryKey, TEntity>
    where TEntity : class, IEntity<TPrimaryKey>
{
    protected IDbContext DBContext { get; }     // 构造注入优先，否则 DbContextScope.Current
    protected Repository();                      // 无参：依赖 AsyncLocal
    public Repository(IDbContext dbContext);     // 显式注入
}
```

`DBContext` 的取值顺序：

1. 构造函数注入的 `IDbContext`；
2. `DbContextScope.Current`（由 `DatabaseContext.CreateScope` 设置）；
3. 都没有 → `InvalidOperationException("IDbContext 不可用。请确保通过 DatabaseContext 获取仓储实例，或在构造函数中显式注入 IDbContext。")`。

注册发生在 `UseDatabaseContext` → `UseRepositories`：

```
对每个 public class（非抽象、非开放泛型）:
    对每个它实现的 IRepository 派生接口:
        services.AddScoped(iface, type)
```

注意：

- 泛型基类 `Repository<,>` 本身不会被注册，所以 `GetRepository<int, User>()` 只有在存在一个实现了 `IRepository<int, User>` 的具体类时才可用；
- 一个仓储类实现多个 `IRepository` 接口时，每个接口都会注册到同一个实现类型；
- 仓储类必须是 `public`。

## 10. 错误信息速查

| 异常消息 | 触发点 |
| --- | --- |
| `连接字符串未设置。请先设置 ConnectionStringAccessor。` | `new DbContext(accessor)` 时 `accessor.ConnectionString` 为空 |
| `IDbContext 不可用。请确保通过 DatabaseContext 获取仓储实例，或在构造函数中显式注入 IDbContext。` | 仓储在无作用域上下文时访问 `DBContext` |
| `没有可用的连接字符串` | 空 `ConnectionStringCollection.GetConnection` |
| `IDbContext 未在当前上下文中初始化。请确保已通过 DatabaseContext 创建作用域。` | 已标记 `[Obsolete]` 的 `DbContextAccessor.Current` |
| `页码必须 >= 1` / `每页条数必须 >= 1` | `BuildPagedSql` 参数校验 |
| `参数名不能为空` | `DbParameters.Add("")` |
| `参数尚未执行，无法读取输出值。请先通过 IDbContext 执行 SQL（如 ExecuteAsync / QueryAsync）。` | `DbParameters.Get<T>` 在 Execute 前调用 |
