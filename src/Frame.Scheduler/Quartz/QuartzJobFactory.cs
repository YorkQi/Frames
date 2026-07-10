using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Frame.Scheduler.Quartz
{
    /// <summary>
    /// Quartz 调度作业工厂
    /// </summary>
    /// <param name="serviceProvider"></param>
    public class QuartzJobFactory(IServiceProvider serviceProvider) : IJobFactory
    {
        /// <summary>
        /// 创建新的作业实例
        /// </summary>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            try
            {
                var jobTypeString = bundle.JobDetail.JobDataMap["JobAssemblyType"] as string
                    ?? throw new ArgumentException("Missing or invalid 'JobAssemblyType' in JobDataMap", nameof(bundle));
                var jobType = Type.GetType(jobTypeString)
                    ?? throw new ArgumentException($"Type not found: '{jobTypeString}'", nameof(bundle));

                var scope = serviceProvider.CreateScope();
                try
                {
                    var jobInstance = scope.ServiceProvider.GetRequiredService(jobType) as ISchedulerJob
                    ?? throw new ArgumentException($"Resolved type '{jobType.FullName}' does not implement required interface ISchedulerJob", nameof(bundle));
                    return new QuartzJobWrapper(jobInstance, scope);
                }
                catch
                {
                    scope.Dispose();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new SchedulerException(
                    $"Failed to instantiate job of type '{bundle.JobDetail.JobType.FullName}'", ex);
            }
        }

        /// <summary>
        /// 释放作业实例
        /// </summary>
        public void ReturnJob(IJob job)
        {
            (job as IDisposable)?.Dispose();
        }
    }
}
