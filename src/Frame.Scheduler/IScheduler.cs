using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public interface IScheduler
    {
        /// <summary>
        /// 开启调度器
        /// </summary>
        /// <returns></returns>
        Task Start();


        /// <summary>
        /// 关闭整个调度器
        /// </summary>
        /// <returns></returns>
        Task End();


        /// <summary>
        /// 添加调度器计划
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        Task Add(SchedulerJobParam option);


        /// <summary>
        /// 暂停调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        Task Pause(SchedulerJobParam opreation);


        /// <summary>
        /// 恢复调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        Task Resume(SchedulerJobParam opreation);


        /// <summary>
        /// 删除调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        Task Remove(SchedulerJobParam opreation);

    }
}
