using Frame.Scheduler;

namespace Job.Tasks
{
    [SchedulerCron("0/5 * * * * ?")]
    public class Test3Scheduler : ISchedulerJob
    {
        public Task ExecuteAsync()
        {
            Console.WriteLine("TestScheduler3");
            return Task.CompletedTask;
        }
    }
}
