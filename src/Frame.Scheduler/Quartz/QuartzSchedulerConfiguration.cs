using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Frame.Core;
using Frame.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Frame.Scheduler.Quartz
{
    public static class QuartzSchedulerConfiguration
    {
        public static ServiceConfigurationContext UseQuartz([NotNull] this ServiceConfigurationContext configuration)
        {
            Check.NotNull(configuration, nameof(configuration));
            configuration.Add(ServiceDescriptor.Singleton<IJobFactory, QuartzJobFactory>());
            configuration.Add(ServiceDescriptor.Singleton<ISchedulerFactory>(new StdSchedulerFactory(
                new NameValueCollection
                {
                    { "quartz.scheduler.instanceName", "Scheduler" },
                    { "quartz.scheduler.instanceId", "Auto" }
                })
            ));

            configuration.Add(ServiceDescriptor.Singleton<IFrameScheduler, QuartzScheduler>());
            return configuration;
        }
    }
}
