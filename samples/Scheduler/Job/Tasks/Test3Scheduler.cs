using Frame.Scheduler;

namespace Job.Tasks
{
    [SchedulerCron("0/5 * * * * ?")]
    public class Test3Scheduler : IScheduler
    {
        public Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
