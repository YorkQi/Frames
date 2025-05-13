using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frame.Core.FrameModules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class FrameModuleAttribute(ServiceLifetime lifetime) : Attribute
    {
        public ServiceLifetime Lifetime { get; set; } = lifetime;
    }
}
