using System;
using System.Collections;
using System.Collections.Generic;

namespace Frame.Core.AutoInjections
{
    public class FrameInjectionCollection : IEnumerable<FrameInjectionCollection>
    {
        public FrameInjectionCollection()
        {
        }
        private readonly List<FrameInjectionCollection> Collection = new();
        public Type Type { get; set; } = null!;

        public Type InterfaceType { get; set; } = null!;

        public void Add(Type type, Type interfaceType)
        {
            Collection.Add(new FrameInjectionCollection
            {
                Type = type,
                InterfaceType = interfaceType
            });
        }
        public void Add(FrameInjectionCollection eventHandlerCollection)
        {
            Collection.AddRange(eventHandlerCollection);
        }

        public IEnumerator<FrameInjectionCollection> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }
    }
}
