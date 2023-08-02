using System;
using System.Collections;
using System.Collections.Generic;

namespace Frame.EventBus
{
    public class EventHandlerCollection : IEnumerable<EventHandlerCollection>
    {
        private List<EventHandlerCollection> eventHandler = new List<EventHandlerCollection>();
        public Type? EnventType { get; set; }

        public Type? EnventHandlerType { get; set; }

        public void Add(Type EnventType, Type EnventHandlerType)
        {
            eventHandler.Add(new EventHandlerCollection()
            {
                EnventType = EnventType,
                EnventHandlerType = EnventHandlerType
            });
        }
        public void Add(EventHandlerCollection eventHandlerCollection)
        {
            eventHandler.AddRange(eventHandlerCollection);
        }

        public IEnumerator<EventHandlerCollection> GetEnumerator()
        {
            return eventHandler.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return eventHandler.GetEnumerator();
        }
    }
}
