using System.Threading;
using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public interface ISchedulerJob
    {
        /// <summary>
        /// 执行定时任务方法
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
