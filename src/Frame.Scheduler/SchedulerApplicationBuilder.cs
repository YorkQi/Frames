using Frame.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class SchedulerApplicationBuilder
    {
        /// <summary>
        /// 传参的方式
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseScheduler(this IApplicationBuilder app, IEnumerable<SchedulerOption> options)
        {
            var schedulerBuilder = app.ApplicationServices.GetService<ISchedulerBuilder>()
                ?? throw new ArgumentNullException(nameof(ISchedulerBuilder));

            foreach (var item in options)
            {
                schedulerBuilder.Add(item);
            }
            schedulerBuilder.Start().GetAwaiter();
            return app;
        }

        /// <summary>
        ///获取的IScheduler的方式
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseScheduler(this IApplicationBuilder app)
        {
            var schedulerBuilder = app.ApplicationServices.GetService<ISchedulerBuilder>()
                ?? throw new ArgumentNullException(nameof(ISchedulerBuilder));


            var options = new List<SchedulerOption>();
            var assembly = Assembly.GetEntryAssembly();
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
                    if (imp == typeof(IScheduler))
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
                                options.Add(new SchedulerOption
                                {
                                    SchedulerName = item.Name,
                                    SchedulerAssmbly = item.FullName,
                                    Cron = schedulerCron.Cron
                                });
                            }
                        }
                    }
                }
            }

            foreach (var item in options)
            {
                schedulerBuilder.Add(item);
            }
            if (options.Any()) schedulerBuilder.Start().GetAwaiter();

            return app;
        }
    }
}
