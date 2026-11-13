namespace Frame.Scheduler
{
    /// <summary>
    /// 作业调度 — 描述一个调度作业的完整运行时信息。
    /// </summary>
    /// <param name="jobName">作业标识名（必须全局唯一）。建议使用业务含义的稳定名称。</param>
    /// <param name="fullName">作业类型的 Type.FullName，用于反射定位</param>
    /// <param name="cronExpression">Cron 执行表达式</param>
    /// <param name="description">作业描述（可选）</param>
    public class JobSchedule(string jobName, string fullName, string cronExpression, string? description = null)
    {
        /// <summary>作业标识名（必须全局唯一）</summary>
        public string JobName { get; } = jobName ?? throw new System.ArgumentNullException(nameof(jobName));

        /// <summary>作业类型完全限定名（Type.FullName），用于通过反射定位类型</summary>
        public string FullName { get; } = fullName ?? throw new System.ArgumentNullException(nameof(fullName));

        /// <summary>Cron 表达式，支持 5 段式和 6 段式两种格式</summary>
        public string CronExpression { get; } = cronExpression ?? throw new System.ArgumentNullException(nameof(cronExpression));

        /// <summary>作业描述（可选）</summary>
        public string? Description { get; } = description;
    }
}
