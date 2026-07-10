using System;

namespace Frame.Scheduler
{
    /// <summary>
    /// 调度作业特性，用于标记需要定时执行的作业类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ScheduledAttribute(string cronExpression, string? description = null) : Attribute
    {
        /// <summary>
        /// Cron表达式，用于定义作业的执行时间
        /// </summary>
        public string CronExpression { get; } = cronExpression;
        /// <summary>
        /// 作业描述，可选，用于提供作业的额外信息
        /// </summary>
        public string? Description { get;} = description;
    }
}
