using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frame.Core.FrameModules
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class FrameInjectionAttribute : Attribute
    {
        public FrameInjectionAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        public ServiceLifetime Lifetime { get; set; }
    }
}
