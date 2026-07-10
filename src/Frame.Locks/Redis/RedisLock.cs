using System;
using System.Threading.Tasks;
using RedLockNet;

namespace Frame.Locks.Redis
{
    internal sealed class RedisLock(IRedLock redLock) : ILock
    {
        private readonly IRedLock _redLock = redLock ?? throw new ArgumentNullException(nameof(redLock));

        public string Resource { get; } = redLock.Resource;

        public string LockId { get; } = redLock.LockId;

        public bool IsAcquired { get; } = redLock.IsAcquired;

        public void Dispose()
        {
            _redLock.Dispose();
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            return _redLock.DisposeAsync();
        }
    }
}
