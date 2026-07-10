using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCrontab;
using Quartz;
using Quartz.Spi;

namespace Frame.Scheduler.Quartz
{
    /// <summary>
    /// Quartz 调度器实现。
    /// 延迟初始化 Quartz.IScheduler，避免构造器中的 async-over-sync 死锁。
    /// </summary>
    public class QuartzScheduler(
        ISchedulerFactory schedulerFactory,
        IJobFactory jobFactory) : IFrameScheduler, IDisposable
    {
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IScheduler? _quartzScheduler;

        private static readonly ConcurrentDictionary<string, Type?> _typeCache = new();

        private static Type? ResolveType(string fullName)
        {
            return _typeCache.GetOrAdd(fullName, name =>
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Type.EmptyTypes; }
                    })
                    .FirstOrDefault(t => t.FullName == name));
        }

        private async Task<IScheduler> GetSchedulerAsync(CancellationToken cancellationToken = default)
        {
            if (_quartzScheduler is not null)
                return _quartzScheduler;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_quartzScheduler is null)
                {
                    _quartzScheduler = await schedulerFactory.GetScheduler(cancellationToken);
                    _quartzScheduler.JobFactory = jobFactory;
                }
                return _quartzScheduler;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scheduler = await GetSchedulerAsync(cancellationToken);
            await scheduler.Start(cancellationToken);
        }

        public async Task AddAsync(JobSchedule schedule, CancellationToken cancellationToken)
        {
            var scheduler = await GetSchedulerAsync(cancellationToken);

            var jobKey = new JobKey(schedule.FullName);
            if (await scheduler.CheckExists(jobKey, cancellationToken))
                return;

            var jobType = ResolveType(schedule.FullName)
                ?? throw new InvalidOperationException($"Type not found: '{schedule.FullName}'");

            var job = JobBuilder.Create<QuartzJobWrapper>()
                .WithIdentity(jobKey)
                .WithDescription(schedule.Description ?? schedule.FullName)
                .UsingJobData("JobAssemblyType", jobType.AssemblyQualifiedName)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{schedule.FullName}.trigger")
                .WithCronSchedule(ToQuartzCron(schedule.CronExpression))
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            if (_quartzScheduler is not null)
            {
                await _quartzScheduler.Shutdown(cancellationToken);
                _quartzScheduler = null;
            }
        }


        /// <summary>
        /// 将 NCrontab 5/6 段式 Cron 表达式转换为 Quartz 6 段式格式。
        /// NCrontab 5 段：分 时 日 月 周
        /// NCrontab 6 段：秒 分 时 日 月 周
        /// Quartz  6 段：秒 分 时 日 月 周（day-of-week 使用 ? 替代 *）
        /// </summary>
        private static string ToQuartzCron(string ncrontabExpression)
        {
            var parts = ncrontabExpression.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            // 通过 NCrontab 解析校验表达式合法性
            if (parts.Length == 5)
            {
                CrontabSchedule.Parse(ncrontabExpression);
                // 补齐秒 → Quartz 6 段
                parts = ["0", .. parts];
            }
            else
            {
                CrontabSchedule.Parse(ncrontabExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            }

            // day-of-week 字段：* → ?
            if (parts[5] == "*")
                parts[5] = "?";

            return string.Join(" ", parts);
        }

        public void Dispose()
        {
            _initLock.Dispose();
        }
    }
}
