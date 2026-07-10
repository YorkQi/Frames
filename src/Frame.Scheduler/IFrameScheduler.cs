using System.Threading;
using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public interface IFrameScheduler
    {
        /// <summary>
        /// 启动调度器
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 添加一个调度任务
        /// </summary>
        Task AddAsync(JobSchedule schedule, CancellationToken cancellationToken);

        /// <summary>
        /// 停止调度器
        /// </summary>
        Task ShutdownAsync(CancellationToken cancellationToken);
    }
}
