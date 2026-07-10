using Frame.Scheduler;

namespace Job.Tasks
{
    public class Test2Scheduler : ISchedulerJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("TestScheduler2");
            return Task.CompletedTask;
        }
    }
}
