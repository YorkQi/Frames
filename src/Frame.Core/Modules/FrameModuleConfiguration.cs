using Frame.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;


namespace Frame.Core.FrameModules
{
    public static class FrameModuleConfiguration
    {
        public static FrameConfiguration UseModule(this FrameConfiguration configuration)
        {
            var assemblyType = configuration.GetAssemblyType();
            foreach (var type in assemblyType.Types)
            {
                if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                {
                    var attrs = type.GetCustomAttributes();
                    foreach (var attr in attrs)
                    {
                        if (attr is FrameModuleAttribute autoInjection)
                        {
                            configuration.Add(new ServiceDescriptor(type, autoInjection.Lifetime));
                        }
                    }
                }
            }
            return configuration;
        }

        public static FrameConfiguration UseApplication(this FrameConfiguration configuration)
        {
            var assemblyType = configuration.GetAssemblyType();
            var interfaceType = typeof(IApplicationService);
            foreach (var type in assemblyType.Types)
            {
                if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                {
                    var imps = type.GetInterfaces();
                    if (imps.Any(t => t.Equals(typeof(IApplicationService))))//取得所有继承IAppcation的类
                    {
                        configuration.Add(ServiceDescriptor.Scoped(imps.First(), type));
                    }
                }
            }
            return configuration;
        }
    }
}
