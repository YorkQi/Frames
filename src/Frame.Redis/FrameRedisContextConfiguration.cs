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
            configuration.Add(ServiceDescriptor.Singleton(redisConnections));
            configuration.Add(ServiceDescriptor.Scoped<IRedisDbContext, RedisDbContext>());
            configuration.Add(ServiceDescriptor.Scoped((provider) =>
                {
                    var redisContext = new TRedisContext();
                    redisContext.Initialize(provider, redisConnections);
                    return redisContext;
                }));
            return configuration;
        }
    }
}
