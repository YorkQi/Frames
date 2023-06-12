using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public interface ISchedulerBuilder
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
        Task Add(SchedulerOption option);


        /// <summary>
        /// 暂停调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        Task Pause(SchedulerOpreation opreation);


        /// <summary>
        /// 恢复调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        Task Resume(SchedulerOpreation opreation);


        /// <summary>
        /// 删除调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        Task Remove(SchedulerOpreation opreation);

    }
}
