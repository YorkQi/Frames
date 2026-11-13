# Frame.Redis

StackExchange.Redis 访问层：`RedisContext` + `IRedisDbContext`。

## 关键位置

`RedisContext.cs`、`RedisDbContexts/RedisDbContext.cs`、`IRedisDbContext.cs`、`RedisConfiguration.cs`（`UseRedisDatabase`）、`RedisConnectionStringCollection.cs`、`Enums/ConnectionStringStrategy.cs`。

## 改动约束

- TFM `netstandard2.1`；`ConnectionMultiplexer` 是 **Singleton**，在 `UseRedisDatabase` 注册期同步 `Connect`——启动即阻塞，连接失败会直接启动失败。
- `RedisDbContext` 是 Scoped，但**不要**在 `Dispose` 里关闭 multiplexer：`Dispose` 现在是空实现（早期 `_redis.Close()` 会掐断全局连接，已修复）。需要关闭连接由宿主在停机时统一处理。
- 所有值统一 `string`；二进制/对象序列化由调用方负责（示例用 `JsonSerializer`）。
- `IRedisDbContext` 的同步与异步方法成对存在，新增命令时两边都要加。
- `Frame.Redis.Enums.ConnectionStringStrategy` 与 `Frame.Databases.Enums.ConnectionStringStrategy` 同名但独立；引用时用完整命名空间或别名。
- `RedisContext.GetDbContext()` 懒建，`SelectDatabase` 影响后续所有命令；不要在同一个 context 里反复切库。

## 验证

`dotnet build src/Frame.Redis/Frame.Redis.csproj`；`RedisConnectionStringCollectionTest` 在 `Test/TestUnit`（不需要真实 Redis）。

文档：[Redis](../docs/concepts/redis.md) · 缓存配方：[recipes](../docs/guide/recipes.md)
