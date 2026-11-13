using Domain.Tasks;
using Frame.Scheduler;
using Infrastructure.DatabaseContexts;
using Microsoft.Extensions.Logging;

namespace Worker.Jobs
{
    /// <summary>
    /// 每 10 分钟扫描一次逾期任务。调度器为每次执行创建独立的 DI 作用域，
    /// 因此这里注入的 DatabaseContext 是该次执行专属的。
    /// </summary>
    [Scheduled("OverdueTaskScan", "0 */10 * * * *", "扫描逾期未完成的任务")]
    public sealed class OverdueTaskScanJob(
        TaskBoardDatabaseContext databaseContext,
        ILogger<OverdueTaskScanJob> logger) : ISchedulerJob
    {
        /// <inheritdoc />
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var repository = databaseContext.GetRepository<ITaskItemRepository>();
            var overdue = await repository.GetOverdueAsync(DateTime.UtcNow, cancellationToken);

            var count = 0;
            foreach (var task in overdue)
            {
                count++;
                logger.LogWarning(
                    "任务逾期：Id={TaskId} 项目={ProjectId} 标题={Title} 截止={DueDate:u}",
                    task.Id,
                    task.ProjectId,
                    task.Title,
                    task.DueDate);
            }

            logger.LogInformation("逾期任务扫描完成，共 {Count} 条。", count);
        }
    }
}
