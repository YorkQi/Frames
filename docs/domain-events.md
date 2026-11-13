# 领域事件

领域事件用于表达「聚合里发生了一件业务上值得关注的事」，例如「订单已支付」。Frames 的模型是：

1. 实体把事件对象加进自己的集合；
2. 工作单元提交时收集这些事件；
3. 提交成功后交给 `IDomainEventDispatcher` 分发。

## 1. 在实体里发布

`Entity<TPrimaryKey>` 实现 `IHasDomainEvents`，提供：

```csharp
public IReadOnlyList<object>? DomainEvents { get; }   // 显式接口实现，只读
public void ClearDomainEvents();                      // 显式接口实现

protected void AddDomainEvent(object domainEvent);    // 子类可调
protected void RemoveDomainEvent(object domainEvent);
protected void ClearDomainEvents();
```

```csharp
public class Order : Entity<int>, IFullAuditedObject
{
    public void Pay()
    {
        // ... 改状态
        AddDomainEvent(new OrderPaidEvent(Id));
    }
}
```

事件类型不必实现 `IEvent`——领域层可以完全不依赖 `Frame.EventBus`。但注意第 5 节的适配规则。

## 2. 分发契约

```csharp
namespace Frame.Core.DomainEvents
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(object domainEvent, CancellationToken cancellationToken = default);
    }
}
```

注册来源：

| 程序集 | 注册 |
| --- | --- |
| `Frame.EventBus` | `UseEventBus()` 注册 `EventBusDomainEventDispatcher`（Scoped） |

没有调用 `UseEventBus()` 时，`IDomainEventDispatcher` 解析为 `null`，事件会被**静默清空**（不抛错，也不记录警告）。需要分发就必须启用事件总线，或自己注册一个实现。

## 3. 收集与分发时机

收集发生在 `Frame.Databases.DatabaseContext.FlushAndDispatchAsync`，由 UOW 的完成处理器触发：

```
[UnitOfWork] 方法返回
  → UOW.CompleteAsync
    → 子 UOW 完成
    → 完成处理器（Frame.Databases 注册）
       1. GetTrackedEntities()                       // EF ChangeTracker: 所有被追踪实体（含 Unchanged）
       2. 从中取走带 DomainEvents 的实体（SaveChanges 之前）
       3. GetChangedEntities() 不为空 → BeginTransactionAsync → SaveChangesAsync → CommitAsync
       4. 提交成功 → 逐个 await dispatcher.DispatchAsync(event)
       5. ClearDomainEvents()
    → OnCompleteCallback
    → 标记完成、清理 Current
```

关键约束：

- **必须经过 UOW**：完全绕开工作单元、只手工 `SaveChangesAsync()` 时不会分发事件；
- **必须用过 `DatabaseContext.GetRepository`**：没有 `IDbContext` 挂到 UOW，就没有完成处理器，事件不会分发；
- **事件来源是「所有被追踪的实体」**：应用服务为了拿自增主键而显式 `SaveChangesAsync` 后，实体虽然变成 `Unchanged`，事件仍会在 UOW 完成时分发；事件只清理一次，重复提交不会重发；
- **在提交之后分发**：回滚时不会外发事件；但「已提交、分发失败」没有补偿，见第 6 节。

## 4. 与事件总线的衔接

```csharp
public sealed class EventBusDomainEventDispatcher(IEventBus eventBus, ILogger<...> logger)
    : IDomainEventDispatcher
{
    public Task DispatchAsync(object domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (domainEvent is not IEvent @event)
        {
            logger.LogWarning("领域事件 {EventType} 未实现 IEvent，已跳过分发。", domainEvent.GetType().FullName);
            return Task.CompletedTask;
        }

        return _eventBus.Push(@event);
    }
}
```

- `Push` 只是把事件放进 `Channel` 并立即返回；真正的处理由 `EventBusHostService` 串行消费（见 [事件总线](eventbus.md)）；
- 因此「分发完成」只代表入队成功，不代表处理器执行完毕；
- 领域事件若要被处理器消费，事件类型必须实现 `IEvent` 并有一个 `IEventHandler<T>` 实现。只在领域层用、不需要宿主处理的内部事件，可以保持普通对象并接受被跳过（或换自定义 dispatcher）。

## 5. 处理器

```csharp
public sealed class OrderPaidEvent : IEvent
{
    public int OrderId { get; set; }
}

public sealed class OrderPaidEventHandler : IEventHandler<OrderPaidEvent>
{
    public Task ExecuteAsync(OrderPaidEvent param)
    {
        // 发邮件、写积分……
        return Task.CompletedTask;
    }
}
```

处理器由 `UseEventBus()` 扫描注册为 Scoped，**按事件的运行期具体类型**解析（`typeof(IEventHandler<>).MakeGenericType(@event.GetType())`）。父类事件不会自动路由到子类处理器。

## 6. 可靠性边界

| 场景 | 结果 |
| --- | --- |
| 事务提交失败 | 回滚，事件不分发；实体上的事件仍在（未清空），下次提交会再次尝试 |
| 提交成功、分发抛异常 | 方法抛出异常；数据**已提交**，事务回滚无效；事件未清空，`EventBus` 未入队的那部分丢失 |
| 提交成功、进程崩溃 | 事件丢失（没有 outbox / 持久化队列） |
| 处理器执行失败 | 由事件总线重试 3 次（指数退避），最终失败只记日志并丢弃 |
| 没有注册 `IDomainEventDispatcher` | 事件被静默清空 |

实践建议：

- 处理器要**幂等**：重复投递、重试、手工补发都可能发生；
- 事件对象只放标识和必要快照，不要放 EF 实体或 `DbContext`；
- 不能丢的事件用 outbox 表 + 后台补偿，当前框架不提供；
- 分发失败的异常会向上冒泡，导致业务方法整体失败——如果不希望影响主流程，在 dispatcher 实现里捕获并记录。

## 7. 与 `IEventBus.Push` 的区别

| | 领域事件 | `IEventBus.Push` |
| --- | --- | --- |
| 来源 | 实体内部 `AddDomainEvent` | 业务代码直接调用 |
| 时机 | UOW 提交成功后自动分发 | 调用即入队 |
| 事务关联 | 与数据变更同一提交点 | 无 |
| 类型要求 | 任意对象（进总线需 `IEvent`） | 必须 `IEvent` |

## 8. 测试

`Test/TestUnit/EntityDomainEventsTests.cs` 覆盖：

- `AddDomainEvent` 后 `DomainEvents` 可见；
- `RemoveDomainEvent` 移除指定事件；
- `ClearDomainEvents` 清空；
- 通过 `IHasDomainEvents` 读取同一集合、并通过接口清空；
- 新实体没有事件时 `DomainEvents` 为 `null`。

分发的集成行为（收集 → 提交 → 分发 → 清空）依赖真实 `DatabaseContext` 与 dispatcher，当前没有端到端测试；改动 `FlushAndDispatchAsync` 时应补。

相关文档：[工作单元与自动事务](unit-of-work.md) · [事件总线](eventbus.md) · [审计字段](auditing.md)
