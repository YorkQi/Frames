using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frame.Core.Locks
{
    public class RedisLockFactory : ILockFactory
    {
        private RedLockFactory factory { get; set; }
        public RedisLockFactory(IEnumerable<string> connections) =>
            factory = RedLockFactory.Create(connections.Select(_ => new RedLockMultiplexer(ConnectionMultiplexer.Connect(_))).ToList(), null);

        public ILock CreateLock(string resource)
        {
            return CreateLock(resource, TimeSpan.FromSeconds(30));
        }

        public async Task<ILock> CreateLockAsync(string resource)
        {
            return await CreateLockAsync(resource, TimeSpan.FromSeconds(30));
        }

        public ILock CreateLock(string resource, TimeSpan expiryTime)
        {
            return new RedisLock(factory.CreateLock(resource, expiryTime, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1)));
        }

        public async Task<ILock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            IRedLock redLock = await factory.CreateLockAsync(resource, expiryTime, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            return new RedisLock(redLock);
        }
    }
}
