using System;

namespace Frame.Scheduler
{
    /// <summary>
    /// 调度作业特性，用于标记需要定时执行的作业类。
    /// <c>jobName</c> 必须全局唯一，作为作业在 Quartz 中的稳定标识，
    /// 避免因类型重命名或命名空间重构导致作业关联丢失。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ScheduledAttribute : Attribute
    {
        /// <summary>作业标识名（用于 Quartz JobKey，必须全局唯一）</summary>
        public string JobName { get; }

        /// <summary>Cron 表达式，用于定义作业的执行时间</summary>
        public string CronExpression { get; }

        /// <summary>作业描述（可选）</summary>
        public string? Description { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="jobName">作业标识名（必须全局唯一）</param>
        /// <param name="cronExpression">Cron 表达式，定义作业执行时间</param>
        /// <param name="description">作业描述（可选）</param>
        public ScheduledAttribute(string jobName, string cronExpression, string? description = null)
        {
            JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
            CronExpression = cronExpression ?? throw new ArgumentNullException(nameof(cronExpression));
            Description = description;
        }
    }
}
