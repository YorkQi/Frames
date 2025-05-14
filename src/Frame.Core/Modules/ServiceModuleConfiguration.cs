using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;


namespace Frame.Core
{
    public static class ServiceModuleConfiguration
    {
        public static ServiceConfigurationContext UseModule(this ServiceConfigurationContext configuration)
        {
            var assemblyTypes = configuration.GetAssemblyType();
            foreach (var type in assemblyTypes)
            {
                if (type.IsPublic && !type.IsInterface && (type.IsClass || type.IsAbstract))
                {
                    var attrs = type.GetCustomAttributes();
                    foreach (var attr in attrs)
                    {
                        if (attr is ServiceModuleAttribute autoInjection)
                        {
                            configuration.Add(new ServiceDescriptor(type, autoInjection.Lifetime));
                        }
                    }
                }
            }
            return configuration;
        }

        public static ServiceConfigurationContext UseApplication(this ServiceConfigurationContext configuration)
        {
            var assemblyTypes = configuration.GetAssemblyType();
            var interfaceType = typeof(IApplicationService);
            foreach (var type in assemblyTypes)
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
