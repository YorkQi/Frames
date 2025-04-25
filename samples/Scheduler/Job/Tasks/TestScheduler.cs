using Frame.Scheduler;

namespace Job.Tasks
{
    public class TestScheduler : ISchedulerJob
    {
        public Task ExecuteAsync()
        {
            Console.WriteLine("TestScheduler");
            return Task.CompletedTask;
        }
    }
}
