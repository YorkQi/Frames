using Quartz;
using System.Threading.Tasks;

namespace Frame.Scheduler.Quartzs
{
    internal class QuartzSchedulerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerJob = (ISchedulerJob)context.MergedJobDataMap.Get("SchedulerJob");
            if (schedulerJob is not null)
            {
                await schedulerJob.ExecuteAsync();
            }
        }

    }
}
