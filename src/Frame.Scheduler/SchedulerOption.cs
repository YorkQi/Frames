namespace Frame.Scheduler
{
    public class SchedulerOption
    {
        /// <summary>
        /// 调度器名称
        /// </summary>
        public string SchedulerName { get; set; } = string.Empty;

        /// <summary>
        /// 调度器组名
        /// </summary>
        public string SchedulerGroupName { get; set; } = "DEFULT";

        /// <summary>
        /// 调度器class
        /// </summary>
        public string SchedulerAssmbly { get; set; } = string.Empty;

        /// <summary>
        /// Cron表达式
        /// </summary>
        public string Cron { get; set; } = string.Empty;
    }
}
