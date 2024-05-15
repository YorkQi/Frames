namespace Frame.Scheduler
{
    public class SchedulerJobParam
    {
        /// <summary>
        /// 定时任务名称
        /// </summary>
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// 定时任务完整类名
        /// </summary>
        public string JobClassName { get; set; } = string.Empty;

        /// <summary>
        /// Cron表达式
        /// </summary>
        public string Cron { get; set; } = string.Empty;
    }
}
