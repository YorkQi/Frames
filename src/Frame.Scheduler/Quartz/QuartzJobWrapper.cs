using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Frame.Scheduler.Quartz
{
    /// <summary>
    /// Quartz IJob 包装器，将 ISchedulerJob 适配到 Quartz 执行模型。
    /// </summary>
    internal class QuartzJobWrapper(ISchedulerJob scheduledJob, IServiceScope scope) : IJob, IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// 执行 Quartz 作业
        /// </summary>
        public Task Execute(IJobExecutionContext context)
        {
            return scheduledJob.ExecuteAsync(context.CancellationToken);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                scope.Dispose();
                (scheduledJob as IDisposable)?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

    }
}
