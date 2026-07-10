using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Frame.Core;
using Frame.Core.Utils;
using Frame.Scheduler.Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Frame.Scheduler
{
    public static class SchedulerConfiguration
    {
        public static ServiceConfigurationContext UseScheduler([NotNull] this ServiceConfigurationContext configuration,
            IEnumerable<JobSchedule>? options = null)
        {
            Check.NotNull(configuration, nameof(configuration));

            var registeredJobNames = new HashSet<string>();
            configuration.RegisterJobsByAttribute(registeredJobNames);
            configuration.RegisterJobsByOptions(options, registeredJobNames);

            configuration.UseQuartz();

            configuration.Add(ServiceDescriptor.Singleton<IHostedService, SchedulerHostedService>());
            return configuration;
        }

        /// <summary>
        /// 扫描标注了 <see cref="ScheduledAttribute"/> 特性的 <see cref="ISchedulerJob"/> 实现类并注册，
        /// 保证 <see cref="JobSchedule.FullName"/> 全局唯一。
        /// </summary>
        private static void RegisterJobsByAttribute(this ServiceConfigurationContext configuration, HashSet<string> registeredJobNames)
        {
            var jobTypes = configuration.GetAssemblyType()
              .Where(t => typeof(ISchedulerJob).IsAssignableFrom(t)
                  && !t.IsInterface
                  && !t.IsAbstract
                  && t.GetCustomAttributes(typeof(ScheduledAttribute), true).Length > 0)
              .ToList();

            foreach (var jobType in jobTypes)
            {
                var attribute = jobType.GetCustomAttribute<ScheduledAttribute>();
                if (attribute != null)
                {
                    var fullName = jobType.FullName!;

                    if (!registeredJobNames.Add(fullName))
                        continue;

                    configuration.Add(ServiceDescriptor.Singleton(new JobSchedule(fullName, attribute.CronExpression, attribute.Description)));
                    configuration.Add(new ServiceDescriptor(jobType, jobType, ServiceLifetime.Scoped));
                }
            }
        }

        /// <summary>
        /// 注册手动配置的 <paramref name="options"/> 调度作业列表，
        /// 保证 <see cref="JobSchedule.FullName"/> 全局唯一。
        /// </summary>
        private static void RegisterJobsByOptions(this ServiceConfigurationContext configuration, IEnumerable<JobSchedule>? options, HashSet<string> registeredJobNames)
        {
            if (options is null)
                return;

            foreach (var option in options)
            {
                var jobType = configuration.GetType(option.FullName);
                if (jobType is not null
                    && registeredJobNames.Add(option.FullName))
                {
                    configuration.Add(ServiceDescriptor.Singleton(option));
                    configuration.Add(new ServiceDescriptor(jobType, jobType, ServiceLifetime.Scoped));
                }
            }
        }
    }
}
