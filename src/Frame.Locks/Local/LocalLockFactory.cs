using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Frame.Locks.Local
{
    internal sealed class LocalLockFactory : ILockFactory
    {
        /// <summary>
        /// 维护 resource → sync object 的映射，确保相同资源名始终使用同一个同步对象，
        /// 从而保证 <see cref="Monitor.TryEnter(object, TimeSpan, ref bool)"/> 的互斥语义正确。
        /// </summary>
        private static readonly ConcurrentDictionary<string, object> _syncRoots = new();

        public ILock CreateLock(string resource)
        {
            return CreateLock(resource, TimeSpan.FromSeconds(30));
        }

        public ILock CreateLock(string resource, TimeSpan expiryTime)
        {
            var syncRoot = _syncRoots.GetOrAdd(resource, _ => new object());
            var @lock = new LocalLock(resource, syncRoot);
            @lock.TryAcquire(expiryTime);
            return @lock;
        }

        public Task<ILock> CreateLockAsync(string resource)
        {
            return CreateLockAsync(resource, TimeSpan.FromSeconds(30));
        }

        public Task<ILock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            var syncRoot = _syncRoots.GetOrAdd(resource, _ => new object());
            var @lock = new LocalLock(resource, syncRoot);
            @lock.TryAcquire(expiryTime);
            return Task.FromResult<ILock>(@lock);
        }
    }
}
