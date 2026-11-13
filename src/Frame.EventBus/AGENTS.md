# Frame.EventBus

进程内事件总线：`Channel` 队列 + 单读者循环 + 指数退避重试。

## 关键位置

`IEventBus.cs`、`EventBusConfiguration.cs`（`UseEventBus`）、`LocalEventBus.cs`、`EventBusHostService.cs`、`Locals/EventBusDomainEventDispatcher.cs`、`EventBusRetryOptions.cs`。

## 改动约束

- TFM `netstandard2.1`；`Channel` 为 `Unbounded` + `SingleReader`，写入端任意线程。
- 按**运行期具体类型**分发（`typeof(IEventHandler<>).MakeGenericType(@event.GetType())`）：发布基类型事件不会命中子类型的处理器，不要改成按声明类型查找。
- `Run` 是串行消费；改并发前先想清楚处理器是否需要顺序保证，以及 `CreateScope` 的生命周期。
- Handler 扫描用 `GetInterface(typeof(IEventHandler<>).FullName)`：一个类实现多个 `IEventHandler<T>` 时只注册一个，已知限制（见 design-notes）。
- `UseEventBus` 同时注册 `IDomainEventDispatcher`（`EventBusDomainEventDispatcher`）；非 `IEvent` 的领域事件会记 Warning 并丢弃，不要假装支持。
- 重试默认 3 次、指数退避，最终失败只记日志丢弃——没有死信队列。

## 验证

`dotnet build src/Frame.EventBus/Frame.EventBus.csproj`；接口级测试在 `Test/TestUnit/EventBusInterfacesTest.cs`。

文档：[事件总线](../docs/concepts/eventbus.md) · [领域事件](../docs/concepts/domain-events.md) · [ADR 0002](../docs/design/adr/0002-domain-events-after-commit.md)
