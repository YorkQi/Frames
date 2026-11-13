# Frame.Scheduler

Quartz 包装：`ISchedulerJob` + `[Scheduled]` + 配置列表两种注册方式。

## 关键位置

`ISchedulerJob.cs`、`IFrameScheduler.cs`、`ScheduledAttribute.cs`、`JobSchedule.cs`、`SchedulerConfiguration.cs`（`UseScheduler`/`UseQuartz`）、`Quartz/`（`QuartzScheduler`、`QuartzJobWrapper`、`QuartzJobFactory`、`SchedulerHostedService`）。

## 改动约束

- TFM `netstandard2.1`；Quartz 的 JobStore 是**内存**的：进程重启后作业全部重建，没有持久化与错过补偿。
- 5 段 cron 自动补 `0` 秒；6 段最后一位 `*` 改成 `?`（Quartz 语义），改动时保持 NCrontab 校验。
- 两种注册方式按 `JobName` 去重：属性扫描先、配置列表后，重复名字只保留先出现的。
- 作业实例由 `QuartzJobFactory` 通过 `CreateScope` 解析并注入；`QuartzJobWrapper.Dispose` 负责释放 scope。`ISchedulerJob` 不要自己持有 Scoped 依赖。
- 类型定位依赖 `AssemblyQualifiedName` + `Type.GetType`（失败再扫 AppDomain）；改程序集名或裁剪程序集会直接失效。
- 属性式注册要求类可实例化且有 `[Scheduled]`；配置列表注册要求 `FullName` 能被 `GetType` 解析。

## 验证

`dotnet build src/Frame.Scheduler/Frame.Scheduler.csproj`；`ScheduledAttributeTest`、`JobDescriptorTest` 在 `Test/TestUnit`。

文档：[调度](../docs/concepts/scheduler.md) · 示例：[Scheduler 示例](../docs/guide/samples.md)
