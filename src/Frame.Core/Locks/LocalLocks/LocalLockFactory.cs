using Frame.Core.Lock.Local;
using Frame.Redis.Locks;
using System;
using System.Threading.Tasks;

namespace Frame.Core.Lock.LocalLocks
{
    public class LocalLockFactory : ILockFactory
    {
        public ILock CreateLock(string resource)
        {
            return CreateLock(resource, TimeSpan.FromSeconds(30));
        }

        public ILock CreateLock(string resource, TimeSpan expiryTime)
        {
            var @lock = new LocalLock(resource);
            @lock.TryAcquire(expiryTime);

            return @lock;
        }

        public Task<ILock> CreateLockAsync(string resource)
        {
            return CreateLockAsync(resource, TimeSpan.FromSeconds(30));
        }

        public Task<ILock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            var @lock = new LocalLock(resource);
            @lock.TryAcquire(expiryTime);
            return Task.FromResult<ILock>(@lock);
        }
    }
}
