using Frame.Redis.Locks;
using Frame.Redis.RedisContexts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frame.Redis
{
    public class RedisContext : IRedisContext
    {
        private IServiceProvider? provider;

        internal void Initialize(IServiceProvider provider, RedisConnection redisConnections)
        {
            this.provider = provider;
        }
        public IRedisDbContext GetContext()
        {
            var redisContext = provider?.GetService<IRedisDbContext>() ?? throw new ArgumentNullException(nameof(IRedisDbContext));
            return redisContext;
        }

        public IRedisLockContext GetLock()
        {
            var redisContext = provider?.GetService<IRedisLockContext>() ?? throw new ArgumentNullException(nameof(IRedisLockContext));
            return redisContext;
        }


    }
}
