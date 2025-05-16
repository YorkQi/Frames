using Frame.Core;
using Frame.Redis.Locks;
using Frame.Redis.RedisContexts;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace Frame.Redis
{
    public static class RedisContextConfiguration
    {
        public static ServiceConfigurationContext UseRedisDatabase<TRedisContext>(
            [NotNull] this ServiceConfigurationContext configuration,
            [NotNull] RedisConnections redisConnections)
            where TRedisContext : RedisContext, new()
        {
            Check.NotNull(configuration, nameof(configuration));
            Check.NotNull(redisConnections, nameof(redisConnections));

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
