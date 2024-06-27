using Frame.Core.AutoInjections;
using Frame.Redis.Locks;
using Frame.Redis.Locks.RedLocks;
using Frame.Redis.RedisContexts;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisDependencyInjection
    {

        public static IServiceCollection AddRedis<TModule>(this IServiceCollection services, Action<RedisContextBuilder> redisBuilder)
            where TModule : class, IModule
        {
            #region 注入Redis库上下文

            RedisContextBuilder builder = new();
            redisBuilder?.Invoke(builder);
            var databaseContexts = builder.GetRedisContext();
            foreach (var databaseContext in databaseContexts)
            {
                if (databaseContext.RedisContextType is not null && databaseContext.RedisContextProvider is not null)
                {
                    services.AddSingleton(databaseContext.RedisContextType, databaseContext.RedisContextProvider);
                }
            }

            #endregion

            #region redis锁

            services.AddSingleton<IRedisLock, RedisRedLock>();

            #endregion
            return services;
        }
    }
}
