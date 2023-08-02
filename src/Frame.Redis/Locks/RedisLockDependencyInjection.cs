using Frame.Redis.Locks;
using Frame.Redis.Locks.RedLocks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisLockDependencyInjection
    {
        public static IServiceCollection AddRedisLock(this IServiceCollection services, RedisOptions option)
        {
            services.AddSingleton(option);
            services.AddSingleton<IRedisLock, RedisRedLock>();
            services.AddSingleton<IRedisLockFactory, RedisRedLockFactory>();
            return services;
        }
    }
}
