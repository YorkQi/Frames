using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Core
{
    public class ServiceConfigurationContext
    {
        readonly List<ServiceDescriptor> serviceDescriptor = [];
        public IReadOnlyList<Type> assemblyType;
        internal ServiceConfigurationContext([NotNull] IReadOnlyList<Type> assemblyType)
        {
            Check.NotNull(assemblyType, nameof(assemblyType));

            this.assemblyType = assemblyType;
        }

        public IReadOnlyList<Type> GetAssemblyType()
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
