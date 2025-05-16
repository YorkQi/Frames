using Frame.Core;
using Frame.Redis.Locks;
using Frame.Redis.RedisContexts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Frame.Redis
{
    public class RedisContext : IRedisContext
    {
        private IServiceProvider? provider;
        private string? redisConnection;

        internal void Initialize(IServiceProvider provider, RedisConnections redisConnections)
        {
            this.provider = provider;
            redisConnection = redisConnections.ToArray().RandomElement();

        }

        public IRedisDbContext GetDbContext()
        {
            var serviceScope = provider?.CreateScope() ?? throw new ArgumentNullException(nameof(provider));
            var redisDbContext = serviceScope.ServiceProvider.GetService<IRedisDbContext>() ?? throw new ArgumentNullException(nameof(IRedisDbContext));
            redisDbContext.Initialize(redisConnection ?? throw new ArgumentNullException(nameof(redisConnection)));
            return redisDbContext;
        }
    }
}
