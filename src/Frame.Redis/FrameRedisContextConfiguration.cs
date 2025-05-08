using Frame.Core;
using Frame.Redis.Locks;
using Frame.Redis.RedisContexts;
using Microsoft.Extensions.DependencyInjection;
namespace Frame.Redis
{
    public static class FrameRedisContextConfiguration
    {
        public static FrameConfiguration UseRedisDatabase<TRedisContext>(this FrameConfiguration configuration, RedisConnection redisConnections)
            where TRedisContext : RedisContext, new()
        {
            configuration.Add(new ServiceDescriptor(typeof(RedisConnection), (provider) => redisConnections, ServiceLifetime.Singleton));
            configuration.Add(new ServiceDescriptor(typeof(IRedisDbContext), typeof(RedisDbContext), ServiceLifetime.Scoped));
            configuration.Add(new ServiceDescriptor(typeof(IRedisLockContext), typeof(RedisLockContext), ServiceLifetime.Scoped));

            configuration.Add(new ServiceDescriptor(typeof(TRedisContext),
                (provider) =>
                {
                    var redisContext = new TRedisContext();
                    redisContext.Initialize(provider, redisConnections);
                    return redisContext;
                }, ServiceLifetime.Scoped));


            return configuration;
        }
    }
}
