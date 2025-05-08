using Frame.Core;
using Frame.Core.FrameModules;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FrameDependencyInjection
    {
        /// <summary>
        /// 全局注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameService(this IServiceCollection services, Action<FrameConfiguration> configuration = null)
        {
            if (configuration is not null)
            {
                var frameConfiguration = new FrameConfiguration();
                configuration?.Invoke(frameConfiguration);
                services.Add(frameConfiguration.GetServiceDescriptor());
            }
            services.AddFrameModule();
            return services;
        }
    }
}
