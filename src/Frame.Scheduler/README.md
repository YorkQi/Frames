# Scheduler调度框架

此框架集成Quartz框架    实现传参方式注入和继承IScheduler接口注入方式进行调度服务

#### 参考文档

```c#
using Frame.Scheduler;

var builder = WebApplication.CreateBuilder(args);

//注入调度计划
builder.Services.AddScheduler();

var app = builder.Build();

#region 第一种方式 获取所有继承IScheduler的公共类，PS:此类必须标记SchedulerCronAttribute特性
app.UseScheduler();
#endregion


#region  第二种方式 传参的方式
List<SchedulerOption> options = new()
{
    new SchedulerOption
    {
        SchedulerName = "Test",
        SchedulerAssmbly = "Job.Tasks.TestScheduler",
        Cron = "0/5 * * * * ?"
    },
    new SchedulerOption
    {
        SchedulerName = "Test2",
        SchedulerAssmbly = "Job.Tasks.Test2Scheduler",
        Cron = "0/5 * * * * ?"
    }
};
app.UseScheduler(options);
#endregion


app.Run();
```


