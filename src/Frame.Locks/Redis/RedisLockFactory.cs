using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Frame.Locks.Redis
{
    internal sealed class RedisLockFactory(IEnumerable<string> connections) : ILockFactory, IDisposable, IAsyncDisposable
    {
        private readonly RedLockFactory _factory = RedLockFactory.Create(
            connections.Select(c => new RedLockMultiplexer(ConnectionMultiplexer.Connect(c))).ToList(),
            null);

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
            return new RedisLock(_factory.CreateLock(resource, expiryTime, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1)));
        }

        public async Task<ILock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            IRedLock redLock = await _factory.CreateLockAsync(resource, expiryTime, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            return new RedisLock(redLock);
        }

        public void Dispose()
        {
            _factory.Dispose();
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            _factory.Dispose();
            GC.SuppressFinalize(this);
            return default;
        }
    }
}
