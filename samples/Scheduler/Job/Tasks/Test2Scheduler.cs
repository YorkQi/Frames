using Frame.Scheduler;
using System.Diagnostics;

namespace Job.Tasks
{
    public class Test2Scheduler : ISchedulerJob
    {
        public Task ExecuteAsync()
        {
            Debug.WriteLine("TestScheduler2");
            return Task.CompletedTask;
        }
    }
}
