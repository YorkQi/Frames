using RedLockNet;
using System;
using System.Threading.Tasks;

namespace Frame.Core.Locks
{
    public class RedisLock : ILock
    {
        private IRedLock redLock;
        public RedisLock(IRedLock redLock)
        {
            Resource = redLock.Resource;
            LockId = redLock.LockId;
            IsAcquired = redLock.IsAcquired;
            this.redLock = redLock;
        }
        public string Resource { get; set; }

        public string LockId { get; set; }

        public bool IsAcquired { get; set; }


        private bool isDisposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                redLock.Dispose();
            }

            isDisposed = true;
        }


        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                await redLock.DisposeAsync();
            }
            isDisposed = true;
        }
    }
}
