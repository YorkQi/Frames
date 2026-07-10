using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Frame.Core.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Core
{
    public class ServiceConfigurationContext
    {
        private readonly List<ServiceDescriptor> serviceDescriptor = [];
        private readonly IReadOnlyList<Type> assemblyType;
        internal ServiceConfigurationContext([NotNull] IReadOnlyList<Type> assemblyType)
        {
            Check.NotNull(assemblyType, nameof(assemblyType));

            this.assemblyType = assemblyType;
        }

        public IReadOnlyList<Type> GetAssemblyType()
        {
            return assemblyType;
        }

        public Type? GetType(string typeFullName)
        {
            return assemblyType.FirstOrDefault(t => t.FullName == typeFullName);
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
