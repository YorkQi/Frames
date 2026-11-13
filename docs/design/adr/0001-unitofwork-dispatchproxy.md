---
title: ADR 0001 用 DispatchProxy 实现声明式工作单元
audience: 贡献者
status: accepted
updated: 2026-11-13
sources:
  - src/Frame.Core/Uow/Interception/UnitOfWorkProxy.cs
---

# ADR 0001 用 `DispatchProxy` 实现声明式工作单元

- 状态：accepted
- 日期：2026-11-13（实现），2026-11-13 记录

## 背景

业务代码希望「方法返回即提交、抛异常即回滚」，而不是每个服务方法手写 `BeginUnitOfWork()` / `CompleteUnitOfWorkAsync()`。框架是类库，不能假设宿主是 ASP.NET Core（不能只靠 MVC 过滤器），`Frame.Core` 又是 `netstandard2.1` 且只依赖 `Microsoft.Extensions.*` 抽象，引入第三方代理库会被所有下游继承。

## 决策

用 BCL 自带的 `DispatchProxy` 在 `UseApplication()` 注册服务时生成接口代理；`UnitOfWorkInterceptor` 读方法级/类级/接口级 `[UnitOfWork]`，开启工作单元，把返回值重写为「先执行、后提交」的任务。

## 备选与取舍

| 备选 | 不选的原因 |
| --- | --- |
| Castle DynamicProxy | 功能强（可代理类），但给核心加一个运行时依赖；且 Core 的 TFM 要迁到 netstandard2.0 以上并非问题，问题在「所有用方被迫接受」 |
| ASP.NET Core 过滤器/中间件 | 只能覆盖 HTTP 请求，Worker、控制台、消息消费者都失效；且框架不应该知道 Web |
| 源码生成器（编译期织入） | 需要消费者额外配置分析器，跨项目体验差；调试困难 |
| 每个服务手写 `BeginUnitOfWork` | 可用但没有「声明式」；漏写时静默不提交，比抛异常更难发现 |

## 后果

- ✅ 零第三方依赖，任何宿主（Web / Worker / 控制台）都能用；接口/接口方法/实现方法/类级特性都支持。
- ⚠️ **只能代理接口**：`UseApplication()` 只注册 `IApplicationService` 派生接口；类内 `new` 出来的对象、`Dispose`、属性访问都不过代理。
- ⚠️ 拦截器类型必须 `public` 且非 `sealed`，否则运行期生成子类失败（已踩过）。
- ⚠️ 每个被代理的服务多一层反射调用；返回值必须重写 `Task`/`Task<T>`/`ValueTask`/`ValueTask<T>`，同步方法只能阻塞等待提交。
- ⚠️ 一个类实现多个 `IApplicationService` 接口时只注册第一个（`ServiceApplicationConfiguration` 的选择），这是代理方案的直接约束。

相关：用法见 [工作单元](../../concepts/unit-of-work.md)；边界见 [design-notes](../design-notes.md)。
