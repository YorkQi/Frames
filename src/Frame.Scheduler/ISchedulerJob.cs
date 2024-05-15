using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public interface ISchedulerJob
    {
        Task ExecuteAsync();
    }
}
