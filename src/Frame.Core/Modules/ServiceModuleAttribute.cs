using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frame.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ServiceModuleAttribute(ServiceLifetime lifetime) : Attribute
    {
        public ServiceLifetime Lifetime { get; set; } = lifetime;
    }
}
