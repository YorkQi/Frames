# 分布式锁

`Frame.Locks` 提供两套 `ILockFactory` 实现：进程内 `Monitor` 锁与基于 [RedLock.net](https://github.com/samcook/RedLock.net) 的 Redis 分布式锁。二者共享同一组接口，切换只需改一行注册代码。

## 1. 契约

```csharp
public interface ILock : IDisposable, IAsyncDisposable
{
    string Resource { get; }      // 锁定的资源名
    string LockId { get; }        // 本次持锁的标识
    bool IsAcquired { get; }      // 是否成功获得锁
}

public interface ILockFactory
{
    ILock CreateLock(string resource);
    Task<ILock> CreateLockAsync(string resource);
    ILock CreateLock(string resource, TimeSpan expiryTime);
    Task<ILock> CreateLockAsync(string resource, TimeSpan expiryTime);
}
```

`ILock` 同时实现同步与异步释放，`Dispose` / `DisposeAsync` 都等价于 `Release`。

## 2. 注册

```csharp
options.UseLock(LockType.Local);                       // 进程内
options.UseLock(LockType.Redis, new LockOptions(new[] { "127.0.0.1:6379" }));
```

签名与行为：

```csharp
public static void UseLock(this ServiceConfigurationContext configuration,
    LockType lockType, LockOptions? options = null)
```

- 注册的是 **Singleton** `ILockFactory`；
- `LockType.Redis` 时要求 `options` 非空，否则 `ArgumentNullException(nameof(options))`；
- `LockType.Local` 时忽略 `options`；
- `LockType` 只有 `Local`、`Redis` 两个值。

```csharp
public class LockOptions
{
    public LockOptions(IEnumerable<string> connections);
    public IEnumerable<string> Connections { get; set; }
}
```

## 3. 使用

```csharp
public class OrderService
{
    private readonly ILockFactory _lockFactory;
    public OrderService(ILockFactory lockFactory) => _lockFactory = lockFactory;

    public async Task PayAsync(int orderId)
    {
        using var handle = await _lockFactory.CreateLockAsync($"order:{orderId}", TimeSpan.FromSeconds(10));
        if (!handle.IsAcquired)
            throw new InvalidOperationException("订单正在处理中，请稍后重试");

        // 临界区
    }
}
```

**关键语义**：`CreateLock` **不会**因为拿不到锁而抛异常，而是返回一个 `IsAcquired == false` 的锁对象。必须检查 `IsAcquired`，否则临界区会在无锁状态下执行。

`using` 释放即可；`IsAcquired == false` 时 `Release` 是空操作。

## 4. 本地实现

`LocalLock` 用 `Monitor.TryEnter(syncRoot, timeout)` 获取、`Monitor.Exit` 释放：

- 每个 `resource` 对应一个共享的 `syncRoot`（静态字典），因此同进程内同名资源互斥；
- 跨进程无效；
- 无参 `CreateLock(resource)` 默认超时 `TimeSpan.FromSeconds(30)`；
- 创建时立即尝试获取，超时后返回 `IsAcquired = false`。

`LocalLockFactory` 的细节：

| 成员 | 说明 |
| --- | --- |
| `static ConcurrentDictionary<string, SyncEntry> _syncRoots` | `resource → (SyncRoot, LastAccess)` |
| `static TimeSpan CleanupInterval = 10 分钟` | 清理周期 |
| `static Timer CleanupTimer` | 每 10 分钟移除 `LastAccess` 超过 10 分钟的条目，防止资源名无限增长导致内存泄漏 |
| `static void ClearCache()` | 清空全部锁对象（测试用） |
| `Dispose()` | 释放 Timer |

### 注意事项

1. **`expiryTime` 在本地实现里是「获取超时」而不是「租期」**。`Monitor` 没有自动过期，持锁方不释放就会一直占用；传入 10 秒只意味着「最多等 10 秒」。
2. **不要跨 `await` 持有本地锁**。`Monitor` 是线程亲和的：`await` 之后续体可能跑在另一个线程，`Monitor.Exit` 会抛 `SynchronizationLockException`。需要异步临界区请用 `SemaphoreSlim` 或改用 Redis 锁。
3. **不可重入**。同一个 `resource` 在已持有时再次 `CreateLock` 会阻塞到超时（`Monitor` 是可重入的，但这里每次创建都会尝试 `TryEnter`，实际上同线程重入会成功——语义上不建议依赖）。

## 5. Redis 实现

`RedisLockFactory` 构造时：

```csharp
RedLockFactory.Create(
    connections.Select(c => new RedLockMultiplexer(ConnectionMultiplexer.Connect(c))).ToList(),
    null);
```

- 每个连接串建立一个 `ConnectionMultiplexer`，**构造时同步连接**；
- `CreateLock(resource, expiry)` 固定 `wait = 10 秒`、`retry = 1 秒`、`expiry = 30 秒`（未指定时）；
- 获取失败返回 `IsAcquired = false`，不抛异常；
- 释放依赖 RedLock 的 token 校验：**只有持有者能释放**，过期后其他实例可重新获取；
- 工厂实现 `IDisposable` / `IAsyncDisposable`，随容器释放时关闭所有多路复用器。

`RedisLock` 是对 `IRedLock` 的薄包装，`Resource` / `LockId` / `IsAcquired` 直接透传。

## 6. 两个实现的差异

| 维度 | Local | Redis |
| --- | --- | --- |
| 作用域 | 单进程 | 跨进程/跨机器 |
| `expiryTime` 含义 | 获取锁的最长等待 | 锁的租期（到期自动释放） |
| 释放安全性 | 无校验（同线程 `Monitor.Exit`） | token 校验，仅持有者可释放 |
| 自动续期 | 无 | 无（RedLock 3.x 需自行续期） |
| 依赖 | 无 | Redis 可达 |
| 适用 | 单实例并发控制、测试 | 多实例部署、幂等入口 |

## 7. 未使用的类型

`LockState { Unlocked, Acquired, NoQuorum, Conflicted, Expired }` 在 `Frame.Locks/Enums/LockState.cs` 中定义，但**没有任何 API 使用它**——`ILock` 只暴露 `bool IsAcquired`，RedLock 的 `NoQuorum`/`Conflicted` 等细分状态在包装层被抹平了。需要区分这些状态时，只能自己包一层 RedLock。
