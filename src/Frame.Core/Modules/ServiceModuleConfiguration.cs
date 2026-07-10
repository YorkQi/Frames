using System.Reflection;
using Microsoft.Extensions.DependencyInjection;


namespace Frame.Core.Modules
{
    public static class ServiceModuleConfiguration
    {
        public static ServiceConfigurationContext UseModule(this ServiceConfigurationContext configuration)
        {
            var assemblyTypes = configuration.GetAssemblyType();
            foreach (var type in assemblyTypes)
            {
                if (!type.IsPublic || !type.IsClass)
                    continue;

                var attr = type.GetCustomAttribute<ServiceModuleAttribute>();
                if (attr is not null)
                {
                    configuration.Add(new ServiceDescriptor(type, attr.Lifetime));
                }
            }
            return configuration;
        }
    }
}
