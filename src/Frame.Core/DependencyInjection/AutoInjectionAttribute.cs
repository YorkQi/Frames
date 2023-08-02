using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frame.Core.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class AutoInjectionAttribute : Attribute
    {
        public AutoInjectionAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        public ServiceLifetime Lifetime { get; set; }
    }
}
