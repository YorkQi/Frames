using Frame.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceDependencyInjection
    {
        /// <summary>
        /// 注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameService([NotNull] this IServiceCollection services,
            Action<ServiceConfigurationContext>? configuration = null)
        {
            Check.NotNull(services, nameof(services));

            var builder = new ServiceConfigurationBuilder();
            var config = builder.Create().Build();
            configuration += config;

            var frameConfiguration = new ServiceConfigurationContext(ReflectionHelper.GetAssemblyAllExportedTypes());
            configuration?.Invoke(frameConfiguration);

            services.Add(frameConfiguration.GetServiceDescriptor());
            return services;
        }
    }
}
