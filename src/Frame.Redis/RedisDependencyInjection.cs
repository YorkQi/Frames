using Frame.Redis.Locks;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisDependencyInjection
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, Action<List<RedisOptions>> options)
        {
            services.Configure(options);
            return services;
        }
    }
}
