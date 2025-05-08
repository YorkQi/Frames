using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Frame.Core.FrameModules
{
    public class FrameServiceDescriptor : IEnumerable<ServiceDescriptor>
    {
        private readonly List<ServiceDescriptor> _collection = [];
        public FrameServiceDescriptor()
        {

        }
        public FrameServiceDescriptor(Type eventType, Type interfaceType, ServiceLifetime lifetime)
        {
            _collection.Add(new ServiceDescriptor(eventType, interfaceType, lifetime));
        }

        public void Add(Type eventType, Type interfaceType, ServiceLifetime lifetime)
        {
            _collection.Add(new ServiceDescriptor(eventType, interfaceType, lifetime));
        }

        public void AddRange(FrameServiceDescriptor handlers)
        {
            _collection.AddRange(handlers);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator<ServiceDescriptor> IEnumerable<ServiceDescriptor>.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
    }
}
