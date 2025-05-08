using Frame.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Frame.Core.FrameModules
{
    public static class FrameInjectionExtension
    {
        public static FrameServiceDescriptor AddFrameServiceDescriptor(this FrameServiceDescriptor collections, params Type[] types)
        {
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        var attrs = type.GetCustomAttributes();
                        foreach (var attr in attrs)
                        {
                            if (attr is FrameInjectionAttribute autoInjection)
                            {
                                collections.Add(null, type, autoInjection.Lifetime);
                            }
                        }
                    }
                }
            }
            return collections;
        }

        public static FrameServiceDescriptor AddApplication(this FrameServiceDescriptor collections, params Type[] types)
        {
            if (types is not null)
            {
                var interfaceType = typeof(IApplication);
                foreach (Type type in types)
                {
                    if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                    {
                        var imps = type.GetInterfaces();
                        if (imps.Any(t => t.Equals(typeof(IApplication))))//取得所有继承IAppcation的类
                        {
                            collections.Add(imps.First(), type, ServiceLifetime.Scoped);
                        }
                    }
                }
            }
            return collections;
        }
    }
}
