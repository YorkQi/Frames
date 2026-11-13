using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Frame.Core;
using Frame.Scheduler.Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Frame.Scheduler
{
    /// <summary>
    /// 任务调度配置扩展方法。
    /// 支持 <see cref="ScheduledAttribute.JobName"/> 作为作业的稳定标识，
    /// 避免类型重命名导致 Quartz 中的作业关联丢失。
    /// 注册去重使用 JobName 而非 FullName，显式指定时更稳定。
    /// </summary>
    public static class SchedulerConfiguration
    {
        /// <summary>
        /// 注册任务调度器（Quartz）。
        /// 自动扫描标注了 <see cref="ScheduledAttribute"/> 的 <see cref="ISchedulerJob"/> 实现，
        /// 并注册手动传入的作业列表。
        /// </summary>
        /// <param name="configuration">框架配置上下文</param>
        /// <param name="options">手动配置的作业列表（可选）</param>
        public static ServiceConfigurationContext UseScheduler([NotNull] this ServiceConfigurationContext configuration,
            IEnumerable<JobSchedule>? options = null)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            var registeredJobNames = new HashSet<string>();
            configuration.RegisterJobsByAttribute(registeredJobNames);
            configuration.RegisterJobsByOptions(options, registeredJobNames);

            configuration.UseQuartz();

            configuration.Add(ServiceDescriptor.Singleton<IHostedService, SchedulerHostedService>());
            return configuration;
        }

        /// <summary>
        /// 扫描标注了 <see cref="ScheduledAttribute"/> 的 <see cref="ISchedulerJob"/> 实现类并注册。
        /// 使用 <see cref="JobSchedule.JobName"/> 作为去重标识。
        /// </summary>
        private static void RegisterJobsByAttribute(this ServiceConfigurationContext configuration, HashSet<string> registeredJobNames)
        {
            var jobTypes = configuration.GetAssemblyTypes()
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

                    if (!registeredJobNames.Add(attribute.JobName))
                        continue;

                    configuration.Add(ServiceDescriptor.Singleton(
                        new JobSchedule(attribute.JobName, fullName, attribute.CronExpression, attribute.Description)));
                    configuration.Add(new ServiceDescriptor(jobType, jobType, ServiceLifetime.Scoped));
                }
            }
        }

        /// <summary>
        /// 注册手动配置的作业列表，使用 <see cref="JobSchedule.JobName"/> 去重。
        /// </summary>
        private static void RegisterJobsByOptions(this ServiceConfigurationContext configuration,
            IEnumerable<JobSchedule>? options, HashSet<string> registeredJobNames)
        {
            if (options is null)
                return;

            foreach (var option in options)
            {
                var jobType = configuration.GetType(option.FullName);
                if (jobType is not null
                    && registeredJobNames.Add(option.JobName))
                {
                    configuration.Add(ServiceDescriptor.Singleton(option));
                    configuration.Add(new ServiceDescriptor(jobType, jobType, ServiceLifetime.Scoped));
                }
            }
        }
    }
}
