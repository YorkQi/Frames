# Frame.Databases

EF Core + Dapper 双通道数据访问：`IDbContext`、`DatabaseContext` 作用域、方言与分页、`DbParameters`。

## 关键位置

| 关注点 | 文件 |
| --- | --- |
| 契约 | `IDBContext.cs`、`IDBContextReader.cs` |
| 作用域与仓储入口 | `DatabaseContext.cs`、`DbContexts/DapperDbContext.cs` |
| EF 通道 | `DbContexts/EfCoreDbContext.cs`、`EFCore/EfCoreContext.cs` |
| 注册 | `DatabaseContextConfiguration.cs`（`UseDatabaseContext`、`UseRepositories`） |
| 方言 | `Dialects/*.cs`、`IDatabaseDialect.cs` |
| 参数 | `DbParameters.cs` |
| 仓储基类 | `Repositories/Repository.cs` |

## 改动约束

- **TFM 是 `net8.0`**，可以用 EF Core 8 API；但接口 `IDBContext` 是公共契约，改签名要同步 `docs/concepts/databases.md` 与 CHANGELOG。
- 注册统一用 `configuration.Add(...)`（不是 `TryAdd`）：同一类型后注册覆盖先注册，这是「宿主可覆盖」的前提。
- `UseDatabaseContext<T>` 是唯一入口：accessor / `IDbContext` / 数据库上下文 / 仓储四类注册都在这里，不要在别处再注册 `IDbContext`；多次调用会重复注册（后注册生效），不要依赖它做合并。
- **写操作延迟持久化**：EF 通道的 `Insert/Update/Delete` 只做变更追踪并返回 `0`，落库靠 `SaveChanges`（通常由 UOW 完成）或仓储 `SaveAsync`。
- 审计填充只在 `EfCoreDbContext.SaveChanges*` 里做；Dapper 通道不填、不软删除，改动时不要声称「已覆盖」。
- `GetTableName` 的静态缓存存的是**加引号后的名字**，与方言绑定；换方言前先看 [design-notes](../docs/design/design-notes.md)。
- 新增方言必须实现 `IDatabaseDialect` 的六个成员，并补分页 SQL 的单测。

## 验证

- `dotnet build src/Frame.Databases/Frame.Databases.csproj`
- 参数/连接串/分页的行为测试在 `Test/TestUnit/`。

文档：[数据库](../docs/concepts/databases.md) · [工作单元](../docs/concepts/unit-of-work.md) · [ADR 0003](../docs/design/adr/0003-auditing-in-savechanges.md)
