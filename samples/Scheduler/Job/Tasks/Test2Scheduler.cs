using Frame.Scheduler;

namespace Job.Tasks
{
    public class Test2Scheduler : IScheduler
    {
        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }
}
