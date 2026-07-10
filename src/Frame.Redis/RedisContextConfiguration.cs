using System.Diagnostics.CodeAnalysis;
using Frame.Core;
using Frame.Core.Utils;
using Frame.Redis.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Redis
{
    public static class RedisContextConfiguration
    {
        public static ServiceConfigurationContext UseRedisDatabase<TRedisContext>(
            [NotNull] this ServiceConfigurationContext configuration,
            [NotNull] RedisConnectionStringCollection redisConnections,
            ConnectionStringStrategy strategy = ConnectionStringStrategy.Random)
            where TRedisContext : RedisContext, new()
        {
            Check.NotNull(configuration, nameof(configuration));
            Check.NotNull(redisConnections, nameof(redisConnections));

            configuration.Add(ServiceDescriptor.Singleton(redisConnections));
            configuration.Add(ServiceDescriptor.Scoped(_ =>
            {
                var redisContext = new TRedisContext
                {
                    RedisConnection = redisConnections.GetConnection(strategy)
                };
                return redisContext;
            }));
            return configuration;
        }
    }
}
