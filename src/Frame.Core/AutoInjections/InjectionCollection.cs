using System;
using System.Collections;
using System.Collections.Generic;

namespace Frame.Core.AutoInjections
{
    public class InjectionCollection : IEnumerable<InjectionCollection>
    {
        public InjectionCollection()
        {
        }
        private readonly List<InjectionCollection> Collection = new();
        public Type Type { get; set; } = null!;

        public Type InterfaceType { get; set; } = null!;

        public void Add(Type type, Type interfaceType)
        {
            Collection.Add(new InjectionCollection
            {
                Type = type,
                InterfaceType = interfaceType
            });
        }
        public void Add(InjectionCollection eventHandlerCollection)
        {
            Collection.AddRange(eventHandlerCollection);
        }

        public IEnumerator<InjectionCollection> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }
    }
}
