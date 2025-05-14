using System;

namespace Frame.Core.Locks
{
    public interface ILock : IDisposable, IAsyncDisposable
    {
        string Resource { get; }
        string LockId { get; }
        bool IsAcquired { get; }
    }
}
