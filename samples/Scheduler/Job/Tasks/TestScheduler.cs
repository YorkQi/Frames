using Frame.Scheduler;
using System.Diagnostics;

namespace Job.Tasks
{
    public class TestScheduler : ISchedulerJob
    {
        public Task ExecuteAsync()
        {
            Debug.WriteLine("TestScheduler");
            return Task.CompletedTask;
        }
    }
}
