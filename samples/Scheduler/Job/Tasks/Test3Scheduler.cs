using Frame.Scheduler;

namespace Job.Tasks
{
    [Scheduled("0/5 * * * * *")]
    public class Test3Scheduler : ISchedulerJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("TestScheduler3");
            return Task.CompletedTask;
        }
    }
}
