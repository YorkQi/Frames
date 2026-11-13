using System.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Frame.Core.Application
{
    public static class ServiceApplicationConfiguration
    {
        public static ServiceConfigurationContext UseApplication(this ServiceConfigurationContext configuration)
        {
            var assemblyTypes = configuration.GetAssemblyTypes();
            var targetInterface = typeof(IApplicationService);
            foreach (var type in assemblyTypes)
            {
                if (!type.IsPublic || !type.IsClass)
                    continue;

                var serviceInterface = type.GetInterfaces()
                    .FirstOrDefault(t => t != targetInterface && targetInterface.IsAssignableFrom(t));
                if (serviceInterface is not null)
                {
                    configuration.Add(ServiceDescriptor.Scoped(serviceInterface, type));
                }
            }
            return configuration;
        }
    }
}
