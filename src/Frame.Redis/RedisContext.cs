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

        internal void Initialize(IServiceProvider provider, RedisConnection redisConnections)
        {
            this.provider = provider;
            redisConnection = RandomConnectionString(redisConnections);

        }
        public IRedisDbContext GetDbContext()
        {
            var serviceScope = provider?.CreateScope() ?? throw new ArgumentNullException(nameof(provider));
            var redisDbContext = serviceScope.ServiceProvider.GetService<IRedisDbContext>() ?? throw new ArgumentNullException(nameof(IRedisDbContext));
            redisDbContext.Initialize(redisConnection ?? throw new ArgumentNullException(nameof(redisConnection)));
            return redisDbContext;
        }
        /// <summary>
        /// 随机取得连接串
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static string RandomConnectionString(RedisConnection connectionString)
        {
            Random random = new();
            var index = random.Next(0, connectionString.Count() - 1);
            var connectionStr = connectionString.Get();
            return connectionStr.ElementAt(index);
        }
    }
}
