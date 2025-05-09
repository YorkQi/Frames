using Frame.Core.Lock.LocalLocks;
using Frame.Redis.Locks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frame.Core.Lock
{
    public static class LockConfiguration
    {
        public static FrameConfiguration UseLock(this FrameConfiguration configuration, LockType lockType, LockConfig? config = null)
        {
            configuration.Add(new ServiceDescriptor(typeof(ILockFactory), (provider) =>
            {
                return lockType switch
                {
                    LockType.Redis => new RedisLockFactory(config?.Conections ?? throw new ArgumentNullException(nameof(config))),
                    _ => new LocalLockFactory()
                };
            }, ServiceLifetime.Singleton));
            return configuration;
        }
    }
}
