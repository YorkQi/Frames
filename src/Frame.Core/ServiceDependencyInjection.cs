using System;
using System.Diagnostics.CodeAnalysis;
using Frame.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Frame.Core
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

            var frameConfiguration = new ServiceConfigurationContext(ReflectionHelper.GetAllAssemblyExportedTypes());
            configuration?.Invoke(frameConfiguration);

            services.Add(frameConfiguration.GetServiceDescriptor());
            return services;
        }
    }
}
