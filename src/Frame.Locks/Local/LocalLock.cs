using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Locks.Local
{
    internal sealed class LocalLock(string resource, object syncRoot) : ILock
    {
        private readonly object _syncRoot = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));
        private bool _isLocked;

        public string Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));

        public string LockId { get; } = Guid.NewGuid().ToString();

        public bool IsAcquired => _isLocked;

        public bool TryAcquire(TimeSpan timeout)
        {
            bool lockTaken = false;
            Monitor.TryEnter(_syncRoot, timeout, ref lockTaken);
            _isLocked = lockTaken;
            return lockTaken;
        }

        public void Release()
        {
            if (_isLocked)
            {
                Monitor.Exit(_syncRoot);
                _isLocked = false;
            }
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            Release();
            GC.SuppressFinalize(this);
            return default;
        }
    }
}
