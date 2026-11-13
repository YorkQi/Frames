# samples/

三个可运行示例：`Webs`（最小 REST API）、`Scheduler/Job`（定时任务）、`TaskBoard`（多宿主项目集）。

## 运行

```bash
dotnet run --project samples/Webs/Web
dotnet run --project samples/Scheduler/Job
dotnet run --project samples/TaskBoard/Web
dotnet run --project samples/TaskBoard/Worker
```

连接串是**占位值**，跑之前必须改成真实地址，并手工建表（SQL 见 [示例工程 §5](../../docs/guide/samples.md#5-数据库与表结构)）。

## 方向约束

- `samples` 可以引用 `src`，**反向绝对禁止**；`src/*` 的 csproj 里不能出现 `..\..\samples`。
- 示例内部保持自己的分层：`TaskBoard` 的 `Web`/`Worker` 只依赖 `Application` 与 `Infrastructure`，领域模型只依赖 `Frame.Core`（事件需要 `IEvent`，因此再引 `Frame.EventBus`）。
- 组合根只有一处：`TaskBoardServiceCollectionExtensions.AddTaskBoard(IConfiguration, configure)`；宿主特有注册（控制器、`UseScheduler`）放在宿主或 `configure` 回调里。
- `samples/*/Tests` 只测领域模型与聚合不变量，不碰数据库/Redis/宿主。

## 改动时

- 端点、表结构、配置键变化 → 同步 [示例文档](../../docs/guide/samples.md) 与 `docs/manifest.json` 的 sources。
- 新增示例项目 → 加入 `Frames.sln`，并在示例文档的「示例一览」表登记。
- 示例测试数变化 → 只改 `manifest.facts.sampleTestCases` 与 `totalTestCases`，再跑 `check-docs.ps1`。

文档：[示例工程](../../docs/guide/samples.md) · [配方](../../docs/guide/recipes.md)
