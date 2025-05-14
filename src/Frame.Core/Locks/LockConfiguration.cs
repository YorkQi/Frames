using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Core.Locks
{
    public static class LockConfiguration
    {
        public static ServiceConfigurationContext UseLock([NotNull] this ServiceConfigurationContext configuration,
            [NotNull] LockType lockType, LockConfig? config = null)
        {
            Check.NotNull(configuration, nameof(configuration));
            Check.NotNull(lockType, nameof(lockType));

            configuration.Add(ServiceDescriptor.Singleton(typeof(ILockFactory), (provider) =>
            {
                return lockType switch
                {
                    LockType.Redis => new RedisLockFactory(config?.Conections ?? throw new ArgumentNullException(nameof(config))),
                    _ => new LocalLockFactory()
                };
            }));
            return configuration;
        }
    }
}
