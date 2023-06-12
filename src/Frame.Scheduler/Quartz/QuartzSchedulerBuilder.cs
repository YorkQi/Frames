using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public class QuartzSchedulerBuilder : ISchedulerBuilder
    {
        private const string SchedulerGroupName = "DEFULT";
        private Quartz.IScheduler? Scheduler { get; set; }

        public QuartzSchedulerBuilder()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            Scheduler = factory.GetScheduler().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 开启调度器
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));
            await Scheduler.Start();
        }

        /// <summary>
        /// 关闭整个调度器
        /// </summary>
        /// <returns></returns>
        public async Task End()
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));
            await Scheduler.Shutdown();
        }

        /// <summary>
        /// 添加调度器计划
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public async Task Add(SchedulerOption option)
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));
            try
            {
                IJobDetail job = JobBuilder.Create<QuartzScheduler>()
                    .WithIdentity(option.SchedulerName, option.SchedulerGroupName)
                    .Build();

                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity($"{option.SchedulerName}.trigger", option.SchedulerGroupName)
                    .ForJob(job)
                    .WithCronSchedule(option.Cron)
                    .StartNow()
                    .Build();

                //写入需要调度的Assmbly
                job.JobDataMap.Add(new KeyValuePair<string, object>("SchedulerAssmbly", $"{option.SchedulerAssmbly}"));

                await Scheduler.ScheduleJob(job, trigger);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message, ex);
            }
        }

        /// <summary>
        /// 暂停调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        public async Task Pause(SchedulerOpreation opreation)
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));
            var (detail, trigger) = await GetSchedulerDetail(opreation);

            var triggerState = await Scheduler.GetTriggerState(trigger.Key);

            if (triggerState == TriggerState.Normal)//正常才暂停
            {
                try
                {
                    await Scheduler.PauseJob(detail.Key);
                    await Scheduler.PauseTrigger(trigger.Key);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// 恢复调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        public async Task Resume(SchedulerOpreation opreation)
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));
            var (detail, trigger) = await GetSchedulerDetail(opreation);

            var triggerState = await Scheduler.GetTriggerState(trigger.Key);

            if (triggerState == TriggerState.Paused)//暂停才恢复
            {
                try
                {
                    await Scheduler.ResumeJob(detail.Key);
                    await Scheduler.ResumeTrigger(trigger.Key);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// 删除调度器计划
        /// </summary>
        /// <param name="opreation"></param>
        /// <returns></returns>
        public async Task Remove(SchedulerOpreation opreation)
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));

            var (detail, _) = await GetSchedulerDetail(opreation);
            try
            {
                await Scheduler.DeleteJob(detail.Key);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message, ex);
            }
        }

        private async Task<(IJobDetail, ITrigger)> GetSchedulerDetail(SchedulerOpreation opreation)
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));

            var detail = await Scheduler.GetJobDetail(new JobKey(opreation.SchedulerName, opreation.SchedulerGroupName));
            if (detail is null) throw new ApplicationException($"未查询的到{opreation.SchedulerName}计划JobDetail");

            var trigger = await Scheduler.GetTrigger(new TriggerKey($"{opreation.SchedulerName}.trigger", opreation.SchedulerGroupName));
            if (trigger is null) throw new ApplicationException($"未查询的到{opreation.SchedulerName}计划Trigger");
            return (detail, trigger);
        }
    }
}
