# Frame.Core

框架内核：实体、仓储契约、工作单元、模块系统、依赖注入引导、审计契约、领域事件契约。

## 关键位置

| 关注点 | 文件 |
| --- | --- |
| 装配入口 | `ServiceDependencyInjection.cs`（`AddFrameService`） |
| 注册上下文 | `ServiceConfigurationContext.cs`（`Add`/`GetAssemblyTypes`/`AddAssemblyOf`） |
| 模块契约 | `Modules/FrameModule.cs`、`Modules/ModuleInitializer.cs`、`Modules/ModuleInitializationHostedService.cs` |
| 工作单元 | `Uow/UnitOfWork.cs`、`Uow/UnitOfWorkManager.cs`、`Uow/Interception/UnitOfWorkProxy.cs` |
| 审计 | `Auditing/`（契约 + `AuditPropertySetter` + `IClock`/`ICurrentUser`） |
| 领域事件 | `DomainEvents/IDomainEventDispatcher.cs`、`Entities/IHasDomainEvents.cs` |

## 改动约束

- **TFM 是 `netstandard2.1`**：不能用 `TimeProvider`、`ArgumentNullException.ThrowIfNull`、`Random.Shared` 等 net6+ API。
- **不能引用其它 `Frame.*` 程序集**：Core 是所有程序集的依赖根，反向引用会造成循环。
- 公共 API 必须有 XML 注释；异常消息用中文，风格与现有一致。
- `UnitOfWorkInterceptor` 必须 `public` 且非 `sealed`（`DispatchProxy` 运行期生成子类）。
- 嵌套 UOW 的完成/回滚处理器统一注册到最外层 `Root`，不要改成登记在当前层。
- `AsyncLocal` 清理必须修改共享 holder 的字段，直接给 `AsyncLocal.Value` 赋 `null` 不会回传到调用方（已踩过）。
- `ServiceConfigurationContext` 新增扩展方法统一命名 `UseXxx`，只注册自己的服务。

## 验证

- 框架行为测试在 `Test/TestUnit/`（`InternalsVisibleTo("TestUnit")` 已开）。
- `dotnet build src/Frame.Core/Frame.Core.csproj` 必须 0 警告 0 错误。

文档：[架构](../docs/concepts/architecture.md) · [工作单元](../docs/concepts/unit-of-work.md) · [模块](../docs/concepts/modules.md) · [ADR 0001](../docs/design/adr/0001-unitofwork-dispatchproxy.md)
