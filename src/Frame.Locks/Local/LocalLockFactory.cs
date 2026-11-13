using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Locks.Local
{
    /// <summary>
    /// 本地锁工厂实现。
    /// 使用 <see cref="Monitor.TryEnter"/> 提供进程内互斥锁。
    /// </summary>
    internal sealed class LocalLockFactory : ILockFactory, IDisposable
    {
        /// <summary>
        /// 同步条目，包含同步对象和最后访问时间。
        /// </summary>
        private sealed class SyncEntry
        {
            public object SyncRoot { get; } = new();
            public DateTime LastAccess { get; set; }
        }

        /// <summary>同步对象映射：resource → SyncEntry</summary>
        private static readonly ConcurrentDictionary<string, SyncEntry> _syncRoots = new();

        /// <summary>清理计时器间隔（超过此时间未访问且无活跃锁的条目将被移除）</summary>
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);

        /// <summary>全局清理计时器，定期回收过期条目</summary>
        private static readonly Timer CleanupTimer = new(CleanupCallback, null,
            CleanupInterval, CleanupInterval);

        /// <summary>
        /// 定时清理回调。遍历所有条目，移除超过 CleanupInterval 未访问的条目。
        /// </summary>
        private static void CleanupCallback(object? state)
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _syncRoots)
            {
                if ((now - kvp.Value.LastAccess) > CleanupInterval)
                {
                    // TryRemove 是为了清理过期条目，失败也无妨（可能被其他线程刚使用）
                    _syncRoots.TryRemove(kvp.Key, out _);
                }
            }
        }

        public ILock CreateLock(string resource)
        {
            return CreateLock(resource, TimeSpan.FromSeconds(30));
        }

        public ILock CreateLock(string resource, TimeSpan expiryTime)
        {
            var entry = _syncRoots.GetOrAdd(resource, _ => new SyncEntry());
            entry.LastAccess = DateTime.UtcNow;

            var @lock = new LocalLock(resource, entry.SyncRoot);
            @lock.TryAcquire(expiryTime);
            return @lock;
        }

        public Task<ILock> CreateLockAsync(string resource)
        {
            return Task.FromResult(CreateLock(resource, TimeSpan.FromSeconds(30)));
        }

        public Task<ILock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            return Task.FromResult(CreateLock(resource, expiryTime));
        }

        /// <summary>主动清理所有缓存条目（通常不需要手动调用）</summary>
        public static void ClearCache()
        {
            _syncRoots.Clear();
        }

        public void Dispose()
        {
            CleanupTimer.Dispose();
        }
    }
}
