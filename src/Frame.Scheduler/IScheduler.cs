using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public interface IScheduler
    {
        Task ExecuteAsync();
    }
}
