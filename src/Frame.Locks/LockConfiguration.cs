using System;
using System.Diagnostics.CodeAnalysis;
using Frame.Core;
using Frame.Core.Utils;
using Frame.Locks.Enums;
using Frame.Locks.Local;
using Frame.Locks.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Locks
{
    public static class LockConfiguration
    {
        public static ServiceConfigurationContext UseLock([NotNull] this ServiceConfigurationContext configuration,
            [NotNull] LockType lockType, LockOptions? options = null)
        {
            Check.NotNull(configuration, nameof(configuration));

            configuration.Add(ServiceDescriptor.Singleton(typeof(ILockFactory), provider =>
            {
                return lockType switch
                {
                    LockType.Redis => new RedisLockFactory(options?.Connections ?? throw new ArgumentNullException(nameof(options))),
                    _ => new LocalLockFactory()
                };
            }));
            return configuration;
        }
    }
}
