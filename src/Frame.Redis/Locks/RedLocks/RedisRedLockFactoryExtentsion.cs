using RedLockNet;

namespace Frame.Redis.Locks.RedLocks
{
    public static class RedLockExtentsion
    {
        public static IRedisLock ToRedisLock(this IRedLock redLock)
        {
            return new RedisRedLock().Initialize(redLock);
        }
    }
}
