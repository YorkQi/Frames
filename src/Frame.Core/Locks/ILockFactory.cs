using System;
using System.Threading.Tasks;

namespace Frame.Redis.Locks
{
    public interface ILockFactory
    {
        ILock CreateLock(string resource);

        Task<ILock> CreateLockAsync(string resource);

        ILock CreateLock(string resource, TimeSpan expiryTime);

        Task<ILock> CreateLockAsync(string resource, TimeSpan expiryTime);
    }
}
