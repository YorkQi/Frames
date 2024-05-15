using Frame.Scheduler;
using System.Diagnostics;

namespace Job.Tasks
{
    [SchedulerCron("0/5 * * * * ?")]
    public class Test3Scheduler : ISchedulerJob
    {
        public Task ExecuteAsync()
        {
            Debug.WriteLine("TestScheduler3");
            return Task.CompletedTask;
        }
    }
}
