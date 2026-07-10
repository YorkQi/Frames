using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frame.Scheduler
{
    /// <summary>
    /// 调度器后台服务
    /// </summary>
    /// <param name="scheduler"></param>
    /// <param name="jobSchedules"></param>
    /// <param name="logger"></param>
    public class SchedulerHostedService(
        IFrameScheduler scheduler,
        IEnumerable<JobSchedule> jobSchedules,
        ILogger<SchedulerHostedService> logger) : BackgroundService
    {
        /// <summary>
        /// 执行调度任务的主方法
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var jobSchedule in jobSchedules)
            {
                try
                {
                    await scheduler.AddAsync(jobSchedule, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "注册调度作业失败: {JobFullName}", jobSchedule.FullName);
                }
            }

            try
            {
                await scheduler.StartAsync(stoppingToken);
                logger.LogInformation("调度器已启动");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "调度器启动失败");
            }
        }

        /// <summary>
        /// 停止调度器服务
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await scheduler.ShutdownAsync(cancellationToken);
            logger.LogInformation("调度器已关闭");
            await base.StopAsync(cancellationToken);
        }
    }
}
