using System;

namespace Frame.Redis.Locks
{
    public interface IRedisLock : IDisposable, IAsyncDisposable
    {
        string Resource { get; }
        string LockId { get; }

        bool IsAcquired { get; }

        RedisLockState Status { get; }

        int ExtendCount { get; }

        string Summary { get; }
    }
}
