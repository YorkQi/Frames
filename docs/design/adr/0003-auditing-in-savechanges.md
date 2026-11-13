---
title: ADR 0003 审计字段在 SaveChanges 前统一填充
audience: 贡献者
status: accepted
updated: 2026-11-13
sources:
  - src/Frame.Databases/DbContexts/EfCoreDbContext.cs
  - src/Frame.Core/Auditing
---

# ADR 0003 审计字段在 EF `SaveChanges` 前统一填充

- 状态：accepted
- 日期：2026-11-13

## 背景

创建时间、创建人、修改时间、修改人、软删除标记这类字段，如果由业务代码手填，必漏；如果放在仓储，Dapper 手写 SQL 与批量接口绕不过去；如果放在应用服务，一样会漏。

框架已有 EF Core 通道（`ChangeTracker` 能精确给出 `Added`/`Modified`/`Deleted`）和 Dapper 通道（手写 SQL，无从拦截）。

## 决策

1. `Frame.Core/Auditing` 只提供契约与填充器：`IHasCreationTime`、`IModificationAuditedObject`、`ISoftDelete` 等 + `IAuditPropertySetter` + `IClock` + `ICurrentUser`；
2. 填充发生在 `EfCoreDbContext.SaveChanges/SaveChangesAsync` 之前：`Added → 创建审计`，`Modified → 修改审计`，`Deleted 且 ISoftDelete → 写删除审计并把状态改为 Modified`；
3. 默认注册用 `TryAdd*`（`SystemClock`、`NullCurrentUser`、`AuditPropertySetter`），宿主可在 `AddFrameService` 之后覆盖；
4. 删除只写删除字段，不顺带更新 `LastModificationTime`。

## 备选与取舍

| 备选 | 不选的原因 |
| --- | --- |
| 仓储层填充 | Dapper 的 `Execute`/`Query` 是任意 SQL；`DeleteBatch` 也曾直接发 `DELETE`，根本拦不住 |
| EF `SaveChangesInterceptor` | 可行，但要 EF Core 8 的拦截器注册链路，且与「双通道」语义无益；直接改 partial 更直白 |
| 应用服务手工填 | 依赖开发者记忆，一定漏；也拿不到软删除的 `Deleted` 状态 |
| 数据库默认值/触发器 | 可移植性差（三套方言），且拿不到「当前用户」 |

## 后果

- ✅ 只要走 EF 通道并 `SaveChanges`，审计一定正确；软删除自动改写，业务代码仍写 `Delete`。
- ⚠️ **仅 EF 通道生效**：Dapper 的 `Insert/Update/Execute` 不会填审计，也不会把物理删除改成软删除。
- ⚠️ 软删除后没有全局查询过滤器，所有读 SQL 要自己加 `IsDeleted = 0`。
- ⚠️ `ICurrentUser` 默认是 `NullCurrentUser`；Web 宿主必须在 `AddFrameService` **之后**覆盖注册，否则拿到空用户。
- ⚠️ 时间统一 UTC；展示层自行转换。

相关：用法见 [审计字段](../../concepts/auditing.md)。
