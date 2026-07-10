namespace Frame.Scheduler
{
    /// <summary>
    /// 作业调度 — 描述一个调度作业的完整运行时信息。
    /// </summary>
    public class JobSchedule(string fullName, string cronExpression, string? description = null)
    {
        /// <summary>
        /// 作业名称（Type.FullName）
        /// </summary>
        public string FullName { get; } = fullName;

        /// <summary>
        /// Cron 表达式，由 NCrontab 解析，支持 5 段式（分 时 日 月 周）和 6 段式（秒 分 时 日 月 周）两种格式。
        /// </summary>
        public string CronExpression { get; } = cronExpression;

        /// <summary>
        /// 作业描述
        /// </summary>
        public string? Description { get; } = description;
    }
}
