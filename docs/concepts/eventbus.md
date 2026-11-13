---
title: 事件总线
audience: 使用者
status: stable
updated: 2026-11-13
sources:
  - src/Frame.EventBus
---

# 事件总线

`Frame.EventBus` 是**进程内**事件总线：发布方 `Push` 一个事件对象，后台宿主服务从无界通道里取出，串行分发给对应 Handler。没有外部消息队列、没有持久化、没有跨进程投递。

## 1. 契约

```csharp
public interface IEvent { }                                  // 标记接口

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task ExecuteAsync(TEvent param);
}

public interface IEventBus
{
    Task Push(IEvent @event);
    internal Task Run(CancellationToken cancellationToken);   // 由宿主服务驱动
}
```

## 2. 注册

```csharp
options.UseEventBus();
// 或调整重试参数
options.UseEventBus(retry => { retry.MaxRetryCount = 5; retry.BaseRetryDelayMs = 500; });
```

`UseEventBus` 做三件事：

1. 扫描所有 public、非抽象、实现 `IEventHandler<>` 的类，按 `IEventHandler<TEvent>` 注册为 **Scoped**；
2. 注册 `IEventBus → LocalEventBus`（Singleton），用 `ActivatorUtilities.CreateInstance` 注入重试配置；
3. 注册 `IHostedService → EventBusHostService`（Singleton）。

`EventBusRetryOptions`：

```csharp
public class EventBusRetryOptions
{
    public int MaxRetryCount { get; set; } = 3;
    public int BaseRetryDelayMs { get; set; } = 1000;
}
```

> Handler 扫描用 `type.GetInterface(typeof(IEventHandler<>).FullName)` 取泛型实参，一个类实现多个 `IEventHandler<T>` 时只会取到其中一个（反射返回顺序不确定），其余事件的 Handler 注册缺失。

## 3. 定义与发布

```csharp
public class OrderPaidEvent : IEvent
{
    public int OrderId { get; set; }
}

public class OrderPaidEventHandler : IEventHandler<OrderPaidEvent>
{
    private readonly ILogger<OrderPaidEventHandler> _logger;
    public OrderPaidEventHandler(ILogger<OrderPaidEventHandler> logger) => _logger = logger;

    public Task ExecuteAsync(OrderPaidEvent param)
    {
        _logger.LogInformation("订单已支付：{OrderId}", param.OrderId);
        return Task.CompletedTask;
    }
}

// 发布
await eventBus.Push(new OrderPaidEvent { OrderId = 42 });
```

`Push` 只是写入无界通道，**立即返回**，不代表已被处理。

## 4. 投递规则

`LocalEventBus.Push` 按**运行期具体类型**构造 Handler 类型：

```csharp
var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());
```

因此：

| 场景 | 结果 |
| --- | --- |
| 发布 `OrderPaidEvent`，Handler 是 `IEventHandler<OrderPaidEvent>` | ✅ 命中 |
| 发布 `DerivedEvent`，只注册了 `IEventHandler<BaseEvent>` | ❌ 不命中（无多态分发） |
| 发布的是接口变量，但实际类型有 Handler | ✅ 命中（取 `GetType()`） |
| 一个事件有多个 Handler | ✅ 全部命中，**串行**依次执行 |
| 没有任何 Handler | ⚠️ 记 Warning 日志后丢弃 |

`ProcessEventAsync` 每次处理都 `CreateScope()` 解析 Handler，所以 Handler 可以注入 Scoped 依赖（`DatabaseContext`、仓储等），用完即释放。表达式树编译的调用委托按 Handler 类型缓存在 `ConcurrentDictionary` 里，避免重复反射。

## 5. 重试与失败

```
for attempt in 0..MaxRetryCount:
    try { await handler.ExecuteAsync(event) ; return }
    catch ex:
        记日志
        if attempt == MaxRetryCount: 放弃（事件丢失）
        await Task.Delay(BaseRetryDelayMs * (1 << attempt))
```

- 默认 3 次重试，退避 1s / 2s / 4s；
- 重试耗尽后**只记日志并丢弃**，没有死信队列；
- 取消（`CancellationToken`）会中止等待并退出循环，事件丢弃；
- 单个 Handler 抛异常不影响其他 Handler（各自独立重试）。

## 6. 并发与顺序

- 通道是 `Channel.CreateUnbounded<EventBusQueueParams>(SingleReader = true, SingleWriter = false)`；
- 只有一个消费循环（`EventBusHostService`），所以**全局串行**处理：前一个事件的 Handler 执行完才处理下一个；
- 吞吐受限于最慢的 Handler。慢 Handler 会阻塞整个总线；
- 多个 `Push` 之间的顺序与入队顺序一致。

## 7. 边界与已知限制

| 限制 | 后果 |
| --- | --- |
| 进程内、无持久化 | 进程崩溃/重启时通道中未处理的事件丢失 |
| 无事务集成 | Handler 与发布方不在同一数据库事务；Handler 失败不会回滚发布方 |
| 无多态分发 | 基类/接口 Handler 收不到派生事件 |
| 无重试持久化 | 重试次数耗尽即丢弃 |
| 全局串行 | 慢 Handler 拖垮吞吐；没有分区/并发度配置 |
| Handler 只扫 public 类 | `internal` Handler 静默不注册 |
| 无 `PublishAsync` 语义区分 | `Push` 即入队，无法得知处理结果 |

如果需要「可靠投递」，应在 Handler 内自行落库（outbox）或接入外部 MQ；本模块只适合日志、通知、缓存失效等可丢失的副作用。
