using System;

namespace Frame.Redis.RedisContexts
{
    internal class RedisContextConfiguration
    {
        internal RedisContextConfiguration(Type redisContextType, Func<IServiceProvider, object> redisContextProvider)
        {
            RedisContextType = redisContextType;
            RedisContextProvider = redisContextProvider;
        }
        internal Type RedisContextType { get; set; }

        internal Func<IServiceProvider, object> RedisContextProvider { get; set; }
    }
}
