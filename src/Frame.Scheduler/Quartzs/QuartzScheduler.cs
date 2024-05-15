using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Frame.Scheduler.Quartzs
{
    internal class QuartzScheduler : IScheduler
    {
        private Quartz.IScheduler? Scheduler { get; set; }
        private const string SchedulerGroupName = "DEFULT";

        public QuartzScheduler()
        {
            StdSchedulerFactory factory = new();
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
        public async Task Add(SchedulerJobParam option)
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));
            try
            {
                IJobDetail job = JobBuilder.Create<QuartzSchedulerJob>()
                    .WithIdentity(option.JobName, SchedulerGroupName)
                    .Build();

                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity($"{option.JobName}.trigger", SchedulerGroupName)
                    .ForJob(job)
                    .WithCronSchedule(option.Cron)
                    .StartNow()
                    .Build();

                var schedulerJob = CreateInstance(option.JobClassName);
                if (schedulerJob is not null)
                {
                    //写入需要调度的Assmbly
                    job.JobDataMap.Add(new KeyValuePair<string, object>("SchedulerJob", schedulerJob));
                    await Scheduler.ScheduleJob(job, trigger);
                }
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
        public async Task Pause(SchedulerJobParam opreation)
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
        public async Task Resume(SchedulerJobParam opreation)
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
        public async Task Remove(SchedulerJobParam opreation)
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

        private async Task<(IJobDetail, ITrigger)> GetSchedulerDetail(SchedulerJobParam opreation)
        {
            if (Scheduler is null) throw new ArgumentNullException(nameof(Scheduler));

            var detail = await Scheduler.GetJobDetail(new JobKey(opreation.JobName, SchedulerGroupName))
                ?? throw new ApplicationException($"未查询的到{opreation.JobName}计划JobDetail");
            var trigger = await Scheduler.GetTrigger(new TriggerKey($"{opreation.JobName}.trigger", SchedulerGroupName));
            return trigger is null ? throw new ApplicationException($"未查询的到{opreation.JobName}计划Trigger") : ((IJobDetail, ITrigger))(detail, trigger);
        }

        private static ISchedulerJob? CreateInstance(string className)
        {
            Assembly? assembly = Assembly.GetEntryAssembly();
            if (assembly is not null)
            {
                Type type = assembly.GetType(className) ?? throw new TypeLoadException($"type '{className}' not found.");
                object instance = Activator.CreateInstance(type) ?? throw new TypeLoadException($"class '{className}' not found.");
                return (ISchedulerJob)instance;
            }
            else
            {
                return null;
            }
        }
    }
}
