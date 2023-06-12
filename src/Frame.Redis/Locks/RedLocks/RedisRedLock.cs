using RedLockNet;
using System;
using System.Threading.Tasks;

namespace Frame.Redis.Locks.RedLocks
{
    public class RedisRedLock : IRedisLock
    {
        private bool isDisposed;
        private IRedLock? redLock;

        private IRedLock RedLock
        {
            get
            {
                if (redLock is null) throw new ArgumentNullException(nameof(RedLock));
                return redLock;
            }
            set { redLock = value; }
        }

        public IRedisLock Initialize(IRedLock redLock)
        {
            RedLock = redLock;
            Resource = redLock.Resource;
            LockId = redLock.LockId;
            IsAcquired = redLock.IsAcquired;
            Status = (RedisLockState)(int)redLock.Status;
            ExtendCount = redLock.ExtendCount;
            Summary = redLock.InstanceSummary.ToString();
            return this;
        }

        public string Resource { get; set; }
        public string LockId { get; set; }

        public bool IsAcquired { get; set; }

        public RedisLockState Status { get; set; }

        public int ExtendCount { get; set; }

        public string Summary { get; set; }

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
                RedLock.Dispose();
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
                await RedLock.DisposeAsync();
            }
            isDisposed = true;
        }
    }
}
