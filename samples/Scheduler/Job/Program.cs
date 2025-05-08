using Frame.Scheduler;

var builder = WebApplication.CreateBuilder(args);

//ע����ȼƻ�
#region ��һ�ַ�ʽ ��ȡ���м̳�IScheduler�Ĺ����࣬PS:���������SchedulerCronAttribute����
builder.Services.AddFrameService(options =>
{
    options.UserScheduler();
});
#endregion

#region  �ڶ��ַ�ʽ ���εķ�ʽ
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
