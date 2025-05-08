using Frame.Scheduler;

var builder = WebApplication.CreateBuilder(args);

//注入调度计划
#region 第一种方式 获取所有继承IScheduler的公共类，PS:此类必须标记SchedulerCronAttribute特性
builder.Services.AddFrameService(options =>
{
    options.UserScheduler();
});
#endregion

#region  第二种方式 传参的方式
List<SchedulerJobParam> schedulerOptions = new()
{
    new SchedulerJobParam
    {
        JobName = "Test",
        JobClassName = "Job.Tasks.TestScheduler",
        Cron = "0/5 * * * * ?"
    },
    new SchedulerJobParam
    {
        JobName = "Test2",
        JobClassName = "Job.Tasks.Test2Scheduler",
        Cron = "0/5 * * * * ?"
    }
};
builder.Services.AddFrameService(options =>
{
    options.UserScheduler(schedulerOptions);
});
#endregion


var app = builder.Build();

app.Run();
