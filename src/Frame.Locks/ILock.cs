using System;

namespace Frame.Locks
{
    public interface ILock : IDisposable, IAsyncDisposable
    {
        string Resource { get; }
        string LockId { get; }
        bool IsAcquired { get; }
    }
}
