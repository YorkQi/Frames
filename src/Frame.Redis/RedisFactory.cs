using Frame.Redis.Locks;
using Frame.Redis.RedisContexts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frame.Redis
{
    public class RedisFactory : IRedisFactory
    {
        private readonly IServiceProvider provider;
        public RedisFactory(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public IRedisContext GetContext<TRedisContext>() where TRedisContext : RedisContext
        {
            var redisContext = provider.GetService<TRedisContext>() ?? throw new ArgumentNullException(nameof(TRedisContext));
            redisContext.InitializeContext();
            return redisContext;
        }

        public IRedisLock GetLock<TRedisContext>(string resource) where TRedisContext : RedisContext
        {
            var redisContext = provider.GetService<TRedisContext>() ?? throw new ArgumentNullException(nameof(TRedisContext));
            redisContext.InitializeLock();
            return redisContext.CreateLock(resource);
        }
    }
}
