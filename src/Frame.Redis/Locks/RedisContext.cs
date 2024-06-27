using Frame.Redis.Locks;
using Frame.Redis.Locks.RedLocks;
using Frame.Redis.RedisContexts;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Redis
{
    public partial class RedisContext : IRedisContext
    {
        private RedLockFactory? RedLockFactory { get; set; }

        public void InitializeLock()
        {
            RedLockFactory = RedLockFactory.Create(connection.Select(_ => new RedLockMultiplexer(ConnectionMultiplexer.Connect(_))).ToList(), null);
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
            if (RedLockFactory is null) throw new ArgumentNullException(nameof(RedLockFactory));
            return RedLockFactory.CreateLock(resource, expiryTime, waitTime, retryTime, cancellationToken).ToRedisLock();
        }

        public async Task<IRedisLock> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null)
        {
            if (RedLockFactory is null) throw new ArgumentNullException(nameof(RedLockFactory));
            IRedLock redLock = await RedLockFactory.CreateLockAsync(resource, expiryTime, waitTime, retryTime, cancellationToken);
            return redLock.ToRedisLock();
        }

    }
}
