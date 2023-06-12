using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Redis.Locks.RedLocks
{
    public class RedisRedLockFactory : IRedisLockFactory
    {
        private RedLockFactory RedLockFactory { get; set; }

        public RedisRedLockFactory(RedisOptions options)
        {

            try
            {
                RedLockFactory = RedLockFactory.Create(options.Select(t => new RedLockMultiplexer(ConnectionMultiplexer.Connect(t))).ToList(), null);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("分布式redis锁连接配置异常", ex);
            }
        }

        public IRedisLock CreateLock(string resource)
        {
            return CreateLock(resource, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
        }

        public async Task<IRedisLock> CreateLockAsync(string resource)
        {
            return await CreateLockAsync(resource, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
        }

        public IRedisLock CreateLock(string resource, TimeSpan expiryTime)
        {
            return CreateLock(resource, expiryTime, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
        }

        public async Task<IRedisLock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            return await CreateLockAsync(resource, expiryTime, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
        }

        public IRedisLock CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null)
        {
            return RedLockFactory.CreateLock(resource, expiryTime, waitTime, retryTime, cancellationToken).ToRedisLock();
        }

        public async Task<IRedisLock> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null)
        {
            IRedLock redLock = await RedLockFactory.CreateLockAsync(resource, expiryTime, waitTime, retryTime, cancellationToken);
            return redLock.ToRedisLock();
        }

    }
}
