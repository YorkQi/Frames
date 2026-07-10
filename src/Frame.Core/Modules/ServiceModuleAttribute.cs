using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Core.Modules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ServiceModuleAttribute(ServiceLifetime lifetime) : Attribute
    {
        public ServiceLifetime Lifetime { get; set; } = lifetime;
    }
}
