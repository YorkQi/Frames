---
title: ADR 0002 领域事件在提交后分发
audience: 贡献者
status: accepted
updated: 2026-11-13
sources:
  - src/Frame.Databases/DatabaseContext.cs
  - src/Frame.Core/DomainEvents
---

# ADR 0002 领域事件在提交后分发，从被追踪实体收集

- 状态：accepted
- 日期：2026-11-13

## 背景

实体在行为方法里 `AddDomainEvent`，但事件什么时候发、由谁发，决定了三件事：处理器里读不读得到已提交的数据、回滚时事件会不会「已发但数据不存在」、以及进程崩溃时的丢失窗口。

同时存在一个实现细节：应用服务为了拿自增主键会显式 `SaveChangesAsync`，实体随即变为 `Unchanged`。如果只从「变更实体」收集事件，这类事件会被永久滞留。

## 决策

1. UOW 完成时，先从 `IDbContext.GetTrackedEntities()`（所有非 `Detached` 实体）收集挂起事件并清空，再 `BeginTransactionAsync → SaveChangesAsync → CommitAsync`；
2. **提交成功后**才逐个 `IDomainEventDispatcher.DispatchAsync`；
3. 提交失败则回滚，事件不发出，也不清空来源（由调用方重试/放弃）；
4. 不实现 outbox。

## 备选与取舍

| 备选 | 不选的原因 |
| --- | --- |
| 提交前在事务内分发 | 处理器读到的是未提交数据；一旦回滚，外部已看到事件但数据消失，比丢事件更难排查 |
| outbox 表 + 后台投递 | 可靠性最好，但需要建表、清理、至少一次投递与去重，超出「小核心」范围；示例也没有迁移机制 |
| 每个仓储方法内即时触发 | 事件与事务边界脱节；批量操作会触发多次 |
| 只从变更实体收集 | 会漏掉「显式 SaveChanges 后变 Unchanged」的事件（已实际发生并修复） |

## 后果

- ✅ 处理器看到的都是已提交数据；回滚不会产生幽灵事件。
- ⚠️ 「已提交、未分发」的窗口存在：进程崩溃会丢事件，没有补偿机制。
- ⚠️ 处理器不在事务内，失败不影响已提交的数据；因此处理器必须幂等。
- ⚠️ 分发是 in-process、串行、失败重试后丢弃（见 [事件总线](../../concepts/eventbus.md)）。
- ⚠️ Dapper 写入、以及不在 UOW 内的保存不会分发；`IDomainEventDispatcher` 未注册时事件被静默清空。
- ⚠️ 收集的是全部被追踪实体，长时间存活的 `DbContext` 可能误发历史事件；框架的 `IDbContext` 生命周期是 Scoped，正常使用不会触发。

相关：用法见 [领域事件](../../concepts/domain-events.md)。
