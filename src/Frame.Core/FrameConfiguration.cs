using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Frame.Core
{
    public class FrameConfiguration
    {
        readonly List<ServiceDescriptor> serviceDescriptor = [];
        public FrameAssemblyType assemblyType;
        internal FrameConfiguration(FrameAssemblyType assemblyType)
        {
            this.assemblyType = assemblyType;
        }

        public FrameAssemblyType GetAssemblyType()
        {
            return assemblyType;
        }

        internal List<ServiceDescriptor> GetServiceDescriptor()
        {
            return serviceDescriptor;
        }

        public void Add(ServiceDescriptor service)
        {
            serviceDescriptor.Add(service);
        }

        public void AddRange(List<ServiceDescriptor> services)
        {
            serviceDescriptor.AddRange(services);
        }
    }
}
