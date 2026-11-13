---
title: 定时任务
audience: 使用者
status: stable
updated: 2026-11-13
sources:
  - src/Frame.Scheduler
---

# 定时任务

`Frame.Scheduler` 把 [Quartz.NET](https://www.quartz-scheduler.net/) 包装成「按 `[Scheduled]` 特性或配置列表自动注册」的调度器。作业实例从 DI 容器解析，因此可以注入仓储、事件总线等依赖。

## 1. 契约

```csharp
public interface ISchedulerJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public interface IFrameScheduler
{
    Task StartAsync(CancellationToken cancellationToken);
    Task AddAsync(JobSchedule schedule, CancellationToken cancellationToken);
    Task ShutdownAsync(CancellationToken cancellationToken);
}
```

作业描述：

```csharp
public class JobSchedule
{
    public JobSchedule(string jobName, string fullName, string cronExpression, string? description = null);
    public string JobName { get; }        // 唯一键
    public string FullName { get; }       // 作业类型的完整名称
    public string CronExpression { get; }
    public string? Description { get; }
}
```

三个必填参数为 `null` 时抛 `ArgumentNullException`。

## 2. 两种注册方式

### 2.1 特性扫描

```csharp
[Scheduled("OrderTimeoutJob", "0 */5 * * * *", "每 5 分钟检查超时订单")]
public class OrderTimeoutJob : ISchedulerJob
{
    public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
}
```

```csharp
options.UseModule();
options.UseScheduler();          // 无参：扫描程序集中的 [Scheduled] 作业
```

扫描条件是：类型在 `ServiceConfigurationContext.GetAssemblyTypes()`（即所有 **public** 类型）中、可实例化、实现 `ISchedulerJob`、带 `[Scheduled]`。同一个 `JobName` 只注册一次。

### 2.2 配置列表

```csharp
var jobs = new List<JobSchedule>
{
    new("TestScheduler",  typeof(TestScheduler).FullName!,  "0/5 * * * * *"),
    new("Test2Scheduler", typeof(Test2Scheduler).FullName!, "0/5 * * * * *"),
};
options.UseScheduler(jobs);
```

`UseScheduler(jobs)` 会通过 `ServiceConfigurationContext.GetType(fullName)` 解析类型；解析不到时**静默跳过**（记不了日志，因为此处还没有 `IServiceProvider`）。

两种方式可以叠加：先扫描特性，再追加配置列表，按 `JobName` 去重。

## 3. 装配

`UseScheduler` 内部：

```csharp
configuration.RegisterJobsByAttribute();   // 特性
configuration.RegisterJobsByOptions(options); // 列表
configuration.UseQuartz();                 // IJobFactory / ISchedulerFactory / IFrameScheduler
// IHostedService -> SchedulerHostedService
```

`UseQuartz` 注册：

| 服务 | 生命周期 | 实现 |
| --- | --- | --- |
| `IJobFactory` | Singleton | `QuartzJobFactory`（从容器解析作业） |
| `ISchedulerFactory` | Singleton | `StdSchedulerFactory`，`instanceName = "Scheduler"`、`instanceId = "Auto"` |
| `IFrameScheduler` | Singleton | `QuartzScheduler` |
| `IHostedService` | Singleton | `SchedulerHostedService` |

**JobStore 是内存实现**：没有配置 `quartz.jobStore.type` 为数据库，也没有集群配置。进程重启后所有作业随内存一起丢失并由启动流程重新注册。

## 4. Cron 表达式

`ToQuartzCron` 负责把「标准 5 段 cron」转成 Quartz 的 6 段格式：

| 输入段数 | 处理 |
| --- | --- |
| 5 段（分 时 日 月 周） | 自动在最前面补 `0`（秒） |
| 6 段（秒 分 时 日 月 周） | 保持原样 |

随后：

- 用 `NCrontab` 校验合法性（6 段时按 `CronFormat.IncludingSeconds`）——非法表达式抛 `FormatException`；
- 把第 6 段（星期）的 `*` 替换为 `?`，因为 Quartz 要求「日」和「星期」至少一个是 `?`。

| 表达式 | 含义 |
| --- | --- |
| `0/5 * * * * *` | 每 5 秒 |
| `0 */5 * * * *` | 每 5 分钟（整分） |
| `0 0 2 * * *` | 每天 02:00 |
| `0 30 8 * * 1-5` | 工作日 08:30 |

> 只支持 5/6 段格式，不支持 Quartz 的 `L`、`W`、`#` 等扩展语法的文档说明；`NCrontab` 校验会拒绝部分 Quartz 专有写法。

## 5. 执行流程

```
SchedulerHostedService.StartAsync (BackgroundService)
  └─ ExecuteAsync:
       foreach JobSchedule: await _scheduler.AddAsync(schedule)   // 失败记日志继续
       await _scheduler.StartAsync()
  └─ 进程内 Quartz 线程触发:
       QuartzJobFactory.NewJob(jobContext)
         ├─ 从 JobDataMap 读 "JobAssemblyType"
         ├─ Type.GetType(...) 解析类型
         ├─ serviceProvider.CreateScope()
         ├─ GetRequiredService(jobType) as ISchedulerJob
         └─ return new QuartzJobWrapper(job, scope)
       QuartzJobWrapper.Execute → job.ExecuteAsync(context.CancellationToken)
       QuartzJobFactory.ReturnJob → scope.Dispose()（幂等包装）
StopAsync → ShutdownAsync → scheduler.Shutdown()
```

`QuartzScheduler.AddAsync` 用 `JobKey(jobName)` 判重：**同名作业已存在时直接返回**，不更新 cron/类型。修改配置后需要重启进程（内存 JobStore 也会随重启清空）。

作业解析失败（类型找不到、未注册、构造抛异常）会抛 `SchedulerException`，由 Quartz 记录；该次触发不会重试。

## 6. 排查清单

| 现象 | 可能原因 | 检查 |
| --- | --- | --- |
| 启动后作业从未执行 | 没有注册 `IHostedService`，或宿主不是 `IHost`（如控制台直接 `new`） | 确认 `UseScheduler` 在 `AddFrameService` 内被调用、进程是 Web/Generic Host |
| 作业没注册 | 类型不是 `public`；没实现 `ISchedulerJob`；`[Scheduled]` 的 `JobName` 重复；`FullName` 写错 | 断点 `RegisterJobsByAttribute` / `RegisterJobsByOptions` |
| 启动即抛 `FormatException` | cron 段数不对或含 NCrontab 不认识的字符 | 用 5 段标准格式先验证 |
| 改了 cron 不生效 | `JobKey` 已存在，`AddAsync` 跳过 | 重启进程，或先 `ShutdownAsync` |
| 作业抛出异常但看不到 | 只有 Quartz 自己的日志 | 在 `ExecuteAsync` 内 try/catch 并记录 |
| 重启后作业重复/丢失 | 内存 JobStore | 这是预期行为；需要持久化请自行配置 Quartz 的 `jobStore` |

## 7. 已知限制

| 限制 | 说明 |
| --- | --- |
| 内存 JobStore | 无持久化、无集群、无故障转移；重启即重建 |
| `JobName` 即唯一键 | 重名静默跳过，不覆盖 |
| 单实例语义 | 多实例部署会各自跑同一作业，需要分布式锁保护（可配合 `Frame.Locks`） |
| 无 misfire 策略配置 | 使用 Quartz 默认策略，停机期间错过的触发不会补跑 |
| 无时区配置 | 使用服务器本地时区 |
| 无执行日志/指标 | 需在作业内部自行埋点 |
| 配置列表缺少类型时报错不可见 | `RegisterJobsByOptions` 静默跳过 |
| 示例工程 | `samples/Scheduler/Job` 里 `UseScheduler()` 被注释、只用了配置列表；`Test3Scheduler` 的 `[Scheduled]` 因此不会注册 |
