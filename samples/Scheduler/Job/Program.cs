using Frame.Core;
using Frame.Core.Modules;
using Frame.Core.Application;
using Frame.Scheduler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFrameService(options =>
{
    options.UseModule();
    options.UseApplication();

    // 方式一：特性扫描 — 自动发现标注了 [Scheduled] 的 ISchedulerJob 实现类
    // 当前 Test3Scheduler 带有 [Scheduled("0/5 * * * * *")]，会被自动注册
    //options.UseScheduler();

    // 方式二：手动配置 — 显式指定作业名称和 Cron 表达式（可与方式一同时使用）
    List<JobSchedule> schedulerOptions =
    [
        new JobSchedule(typeof(Job.Tasks.TestScheduler).FullName!, "0/5 * * * * *", "Test"),
        new JobSchedule(typeof(Job.Tasks.Test2Scheduler).FullName!, "0/5 * * * * *", "Test2")
    ];
    options.UseScheduler(schedulerOptions);
});

var app = builder.Build();

app.Run();
