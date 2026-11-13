---
title: ADR 0004 示例采用单一组合根与多宿主
audience: 贡献者
status: accepted
updated: 2026-11-13
sources:
  - samples/TaskBoard/Infrastructure
---

# ADR 0004 示例采用单一组合根 `AddTaskBoard` + 多宿主

- 状态：accepted
- 日期：2026-11-13

## 背景

`TaskBoard` 要同时演示 Web 宿主与后台 Worker 宿主共享同一套领域与装配。若两个 `Program.cs` 各写一遍 `AddFrameService`，两处会逐渐漂移（漏 `UseEventBus`、连接串键名不一致），示例本身会变成反例。

## 决策

1. `Infrastructure` 里暴露唯一组合根：

   ```csharp
   public static IServiceCollection AddTaskBoard(
       this IServiceCollection services,
       IConfiguration configuration,
       Action<ServiceConfigurationContext>? configure = null);
   ```

2. 组合根内部完成 `UseModule / UseApplication / UseDatabaseContext<...> / UseEventBus / UseLock`，并在最后执行 `configure?.Invoke(o)`；
3. 宿主只调 `AddTaskBoard`，再追加自己特有的东西：Web 加控制器与异常过滤器，Worker 在 `configure` 里 `UseScheduler()`；
4. Web 与 Worker 各有一份 `appsettings.json`，键名固定为 `TaskBoard:Connections:Database`。

## 备选与取舍

| 备选 | 不选的原因 |
| --- | --- |
| 每个宿主各写一遍注册 | 立刻出现两份事实源；示例读者会复制错的那份 |
| 共享 `Startup` 基类 | Web 与 Worker 的宿主 API 不同（`WebApplicationBuilder` vs `HostApplicationBuilder`），基类会退化成 `IServiceCollection` 扩展，等于本方案 |
| 只提供 `IConfiguration` 绑定的 Options 对象 | 能统一配置，但无法表达「Worker 要 Scheduler、Web 不要」的差异；仍需回调 |
| 把组合根放进 `Application` | `Application` 不应知道数据库连接键与宿主能力；且会让 `Application` 反向依赖 `Databases` |

## 后果

- ✅ 装配只有一处；两个宿主行为一致，差异显式写在 `configure` 回调里。
- ⚠️ `Application` 必须引用 `Infrastructure`（为拿到 `TaskBoardDatabaseContext` 类型调 `GetRepository`），这是示例的分层妥协；框架的 `DatabaseContext` 没有暴露通用接口，见 [design-notes](../design-notes.md)。
- ⚠️ 组合根隐藏了注册细节，读者必须同时看 `AddTaskBoard` 与宿主 `Program.cs` 才能理解全貌；文档在 [示例工程 §7](../../guide/samples.md#7-项目集方案-taskboard) 里给出了完整调用链。
- ⚠️ 该模式只用于示例，不是框架 API；不要把它当成「Frames 要求的分层模板」。

相关：完整分层与端点见 [示例工程](../../guide/samples.md)。
