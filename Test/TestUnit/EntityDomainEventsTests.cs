using Frame.Core.Entities;
using Xunit;

namespace TestUnit
{
    public class EntityDomainEventsTests
    {
        private sealed class TestEntity : Entity<int>
        {
            public void RaiseEvent(object domainEvent) => AddDomainEvent(domainEvent);
            public void RemoveEvent(object domainEvent) => RemoveDomainEvent(domainEvent);
            public void ClearAllEvents() => ClearDomainEvents();
        }

        [Fact]
        public void DomainEvents_IsNullBeforeAnyEventAdded()
        {
            var entity = new TestEntity();

            Assert.Null(entity.DomainEvents);
        }

        [Fact]
        public void AddDomainEvent_AppendsEventsInOrder()
        {
            var entity = new TestEntity();
            var first = new object();
            var second = new object();

            entity.RaiseEvent(first);
            entity.RaiseEvent(second);

            Assert.NotNull(entity.DomainEvents);
            Assert.Equal(new[] { first, second }, entity.DomainEvents!);
        }

        [Fact]
        public void RemoveDomainEvent_RemovesMatchingEvent()
        {
            var entity = new TestEntity();
            var first = new object();
            var second = new object();
            entity.RaiseEvent(first);
            entity.RaiseEvent(second);

            entity.RemoveEvent(first);

            Assert.Equal(new[] { second }, entity.DomainEvents!);
        }

        [Fact]
        public void ClearDomainEvents_EmptiesCollection()
        {
            var entity = new TestEntity();
            entity.RaiseEvent(new object());

            entity.ClearAllEvents();

            Assert.NotNull(entity.DomainEvents);
            Assert.Empty(entity.DomainEvents!);
        }

        [Fact]
        public void IHasDomainEvents_ExposesSameCollection()
        {
            var entity = new TestEntity();
            var domainEvent = new object();
            entity.RaiseEvent(domainEvent);

            var contract = Assert.IsAssignableFrom<IHasDomainEvents>(entity);

            Assert.Equal(entity.DomainEvents, contract.DomainEvents);
            contract.ClearDomainEvents();
            Assert.Empty(contract.DomainEvents!);
        }
    }
}
