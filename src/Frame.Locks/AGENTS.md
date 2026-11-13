# Frame.Locks

本地锁（`Monitor`）与 RedLock 分布式锁的统一抽象。

## 关键位置

`ILock.cs`、`ILockFactory.cs`、`LockConfiguration.cs`（`UseLock`）、`LockOptions.cs`、`Locals/`（`LocalLock`、`LocalLockFactory`）、`Redis/`（`RedisLock`、`RedisLockFactory`）。

## 改动约束

- TFM `netstandard2.1`；`LockType` 只有 `Local` / `Redis`。
- `CreateLock` 返回的是「已尝试获取」的锁：`IsAcquired == false` 不抛异常，调用方必须自己判断；`using` 释放时 `Monitor.Exit` 必须与 `TryEnter` 成对（`LocalLock` 内部已保证）。
- 本地锁的 `expiryTime` 只是 `Monitor.TryEnter` 的等待上限；Redis 锁的 expiry 是真正的租期，两者语义不同，别在文档里混写。
- `LocalLockFactory` 持静态字典 + 10 分钟清理定时器；改缓存策略时同步更新 [locks 文档](../docs/concepts/locks.md) 的「内存」一节。
- `UseLock(LockType.Redis)` 必须传 `LockOptions.Connections`，否则注册时就抛 `ArgumentNullException`；连接在注册期同步建立。

## 验证

`dotnet build src/Frame.Locks/Frame.Locks.csproj`；行为测试在 `Test/TestUnit`（`LockEnumsTest`、`LockOptionsTest`）。

文档：[锁](../docs/concepts/locks.md)
