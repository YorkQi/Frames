using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Redis.Locks
{
    public interface IRedisLockFactory
    {
        IRedisLock CreateLock(string resource);

        Task<IRedisLock> CreateLockAsync(string resource);

        IRedisLock CreateLock(string resource, TimeSpan expiryTime);

        Task<IRedisLock> CreateLockAsync(string resource, TimeSpan expiryTime);

        IRedisLock CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null);

        Task<IRedisLock> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null);

    }
}
