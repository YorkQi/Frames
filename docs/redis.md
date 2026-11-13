# Redis 访问

`Frame.Redis` 是 [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) 的一层薄封装：一个 `RedisContext` 负责连接池与作用域，`IRedisDbContext` 把常用命令按数据类型分组暴露成同步 + 异步成对的方法。

## 1. 注册

```csharp
options.UseRedisDatabase<CommandRedisContext>(
    new RedisConnectionStringCollection("127.0.0.1:6379"),
    ConnectionStringStrategy.RoundRobin);
```

```csharp
public class CommandRedisContext : RedisContext { }   // 空子类，用于区分读写实例
```

`UseRedisDatabase<TRedisContext>` 的注册行为：

1. 把 `RedisConnectionStringCollection` 实例注册为 Singleton；
2. **在注册阶段（启动时）同步调用 `ConnectionMultiplexer.Connect(...)`**，把结果注册为 Singleton；
3. 把 `TRedisContext` 注册为 Scoped，构造时注入该 `ConnectionMultiplexer`。

因此 Redis 不可达时**应用启动就会失败**，而不是首次访问时失败。

连接串集合：

```csharp
public class RedisConnectionStringCollection : IEnumerable<string>
{
    public RedisConnectionStringCollection(params string[] connections);
    public void Add(params string[] connections);
    public string GetConnection(ConnectionStringStrategy strategy);
    public string GetConnection();     // 默认 RoundRobin
}
```

策略：`Random` / `RoundRobin`。空集合时抛 `InvalidOperationException("没有可用的 Redis 连接字符串")`。

> `Frame.Redis.Enums.ConnectionStringStrategy` 与 `Frame.Databases.Enums.ConnectionStringStrategy` 是**两个同名枚举**（值也相同：`Random`、`RoundRobin`）。同一文件里同时 `using` 两个命名空间会歧义，需要写全名或用别名。

## 2. 使用

```csharp
public class CacheService
{
    private readonly CommandRedisContext _redis;
    public CacheService(CommandRedisContext redis) => _redis = redis;

    public async Task<User?> GetUserAsync(int id)
    {
        var db = _redis.GetDbContext();
        var json = await db.StringGetAsync($"user:{id}");
        return json is null ? null : JsonSerializer.Deserialize<User>(json);
    }

    public async Task SetUserAsync(User user, TimeSpan ttl)
    {
        var db = _redis.GetDbContext();
        await db.StringSetAsync($"user:{user.Id}", JsonSerializer.Serialize(user), ttl);
    }
}
```

`RedisContext.GetDbContext()`：

- 懒创建 `RedisDbContext`，同一 `RedisContext` 实例内复用；
- `ConnectionMultiplexer` 为 `null` 时抛 `InvalidOperationException("Redis ConnectionMultiplexer 未设置。请确保已调用 UseRedisDatabase 注册 Redis 服务。")`；
- `RedisContext` 是 Scoped，随请求结束释放。

## 3. 命令覆盖

`IRedisDbContext` 的所有方法都返回 `string`（null 表示键不存在），并提供 `...Async` 版本。分组如下：

| 分组 | 方法 |
| --- | --- |
| Key | `KeyExists`、`DeleteKey`、`KeyExpire`、`KeyTimeToLive`、`KeyPersist`、`KeyRename` |
| String | `Set`、`SetNx`、`Get`、`GetSet`、`Increment`、`Decrement`、`IncrementFloat`、`StringLength`、`StringAppend`、`MGet`、`MSet` |
| List | `LeftPush`、`LeftPop`、`RightPush`、`RightPop`、`ListLength`、`ListRange`、`ListRemove`、`ListGetByIndex`、`ListSetByIndex`、`ListTrim` |
| Set | `SetAdd`、`SetRemove`、`SetContains`、`SetLength`、`SetMembers`、`SetRandomMember`、`SetRandomMembers`、`SetUnion`、`SetIntersect`、`SetDifference` |
| Hash | `HashSet`（单值/字典）、`HashGet`（单字段/多字段）、`HashGetAll`、`HashExists`、`HashDelete`（单/多）、`HashKeys`、`HashValues`、`HashLength`、`HashIncrement`、`HashIncrementFloat` |
| SortedSet | `SortedSetAdd`、`SortedSetRemove`、`SortedSetRemoveRangeByScore`、`SortedSetRemoveRangeByRank`、`SortedSetScore`、`SortedSetIncrement`、`SortedSetDecrement`、`SortedSetLength`、`SortedSetLengthByScore`、`SortedSetRangeByScore`、`SortedSetRangeByRank`、`SortedSetRangeByScoreWithScores` |
| Pub/Sub | `Publish` |

`RedisDbContext` 还提供 `SelectDatabase(int database)` 切换逻辑库（默认 0）。注意它只改变当前上下文持有的 `IDatabase` 引用，底层 `ConnectionMultiplexer` 是共享的。

## 4. 已知问题

### 4.1 Scope 释放与全局连接（已修复）

历史问题：`RedisDbContext.Dispose()` 曾调用 `_redis.Close()`，而 `_redis` 是**单例** `ConnectionMultiplexer`。第一个请求结束、Scoped `RedisContext` 被释放时，整个应用的 Redis 连接就被关闭，第二次请求开始报 `ObjectDisposedException`。

现在 `RedisDbContext.Dispose()` 是空实现，连接由 DI 容器的 Singleton 管理，可以安全地按请求注入 `RedisContext`：

```csharp
public sealed class UserService(CommandRedisContext redis) : IApplicationService
{
    public Task<string?> GetNameAsync(int id) =>
        redis.GetDbContext().StringGetAsync($"frames:users:{id}");
}
```

残留边界：

- 应用停止时由容器释放 multiplexer，没有显式 `Close`/优雅排空；
- `RedisContext` 每次 `GetDbContext()` 都从共享 multiplexer 取一个 `IDatabase`，不要自己 `Close` 它；
- 同一个进程里手动 `new` 多个 multiplexer 时，释放责任仍在调用方。

### 4.2 其余限制

| 限制 | 说明 |
| --- | --- |
| 值统一为 `string` | 不支持 `byte[]`/对象序列化；需要自己 `JsonSerializer` |
| 无 Pipeline / Batch / Transaction | 每条命令一次往返；批量场景性能受限 |
| 无 Stream / Geo / Bitmap / HyperLogLog | 只覆盖五种基础结构 |
| 无分布式锁 | 锁在 `Frame.Locks`，不在本模块 |
| 无订阅消费 | 只有 `Publish`，没有 `Subscribe` 封装 |
| 连接期同步阻塞 | `UseRedisDatabase` 在注册时 `Connect`，启动耗时受网络影响 |
| 无重连/超时配置 | 使用 StackExchange.Redis 默认值，无法从 `UseRedisDatabase` 传入 `ConfigurationOptions` |
| 逻辑库共享 | `SelectDatabase` 只影响当前 `IDatabase` 引用 |

## 5. 与数据库模块的关系

`Frame.Redis` 与 `Frame.Databases` 完全独立：前者不参与 UOW，不做事务，也不会在 `SaveChanges` 时刷新缓存。需要「写库 + 删缓存」一致性时，应在应用服务里显式编排，或通过事件总线发一个缓存失效事件（注意事件总线也不保证可靠投递）。
