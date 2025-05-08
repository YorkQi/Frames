using Frame.Core;
using Frame.Scheduler;
using Frame.Scheduler.Quartzs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SchedulerDependencyInjection
    {
        public static FrameConfiguration UserScheduler(this FrameConfiguration services, IEnumerable<SchedulerJobParam>? options = null)
        {
            IScheduler scheduler = new QuartzScheduler();

            List<SchedulerJobParam> schedulerOptions = new();
            #region 传入定时计划任务部分
            if (options is not null && options.Any())
            {
                schedulerOptions.AddRange(options);
            }
            #endregion

            #region 取得所有继承Frame.Scheduler.IScheduler部分
            var assembly = Assembly.GetEntryAssembly();
            if (assembly is not null)
            {
                var types = assembly.GetExportedTypes();
                foreach (var item in types)
                {
                    Type[]? imps = item.GetInterfaces();
                    if (imps is null)
                    {
                        continue;
                    }
                    foreach (var imp in imps)
                    {
                        if (imp == typeof(ISchedulerJob))
                        {
                            var attributes = item.GetCustomAttributes();
                            if (attributes is null)
                            {
                                continue;
                            }
                            foreach (var attribute in attributes)
                            {
                                if (attribute is SchedulerCronAttribute schedulerCron)
                                {
                                    if (item.FullName is not null)
                                    {
                                        schedulerOptions.Add(new SchedulerJobParam
                                        {
                                            JobName = item.Name,
                                            JobClassName = item.FullName,
                                            Cron = schedulerCron.Cron
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            schedulerOptions = schedulerOptions.Distinct().ToList();//防止重复

            foreach (var item in schedulerOptions)
            {
                scheduler.Add(item);
            }
            scheduler.Start().GetAwaiter();
            return services;
        }
    }
}
