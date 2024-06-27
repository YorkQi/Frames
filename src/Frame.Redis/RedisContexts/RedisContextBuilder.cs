using Frame.Redis.Locks;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Frame.Redis.RedisContexts
{
    public class RedisContextBuilder
    {
        private readonly List<RedisContextConfiguration> RedisContexts = default!;

        internal IEnumerable<RedisContextConfiguration> GetRedisContext()
        {
            return RedisContexts;
        }

        public void UseRedisDatabase<TRedisContext>(RedisConnection redisConnection) where TRedisContext : RedisContext, new()
        {
            var redisContextType = typeof(TRedisContext);
            RedisContexts.Add(new RedisContextConfiguration(
               redisContextType, (provider) =>
               {
                   ConstructorInfo constructor = redisContextType.GetConstructor(new[] { typeof(RedisConnection) }) ?? throw new ArgumentNullException(nameof(TRedisContext));
                   var redisContext = (TRedisContext)constructor.Invoke(new object[] { redisConnection });
                   return redisContext;
               }));
        }
    }
}
