using Frame.Redis.Locks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Lock.Local
{
    public class LocalLock : ILock
    {
        private readonly object _syncRoot;
        private bool _isLocked;
        public LocalLock(object syncRoot)
        {
            Resource = syncRoot.ToString();
            LockId = Guid.NewGuid().ToString();
            _syncRoot = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));
        }

        public string Resource { get; set; }

        public string LockId { get; set; }

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
        public async ValueTask DisposeAsync()
        {
            Release();
            GC.SuppressFinalize(this);
        }
    }
}
