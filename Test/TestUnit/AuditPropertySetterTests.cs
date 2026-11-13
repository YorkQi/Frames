using Frame.Core.Auditing;
using Xunit;

namespace TestUnit
{
    public class AuditPropertySetterTests
    {
        private sealed class FixedClock : IClock
        {
            public DateTime Now { get; set; } = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        }

        private sealed class FakeCurrentUser : ICurrentUser
        {
            public string? Id { get; set; } = "user-1";
            public string? Name { get; set; } = "tester";
        }

        private sealed class FullAuditedEntity : IFullAuditedObject
        {
            public DateTime CreationTime { get; set; }
            public string? CreatorId { get; set; }
            public DateTime? LastModificationTime { get; set; }
            public string? LastModifierId { get; set; }
            public bool IsDeleted { get; set; }
            public DateTime? DeletionTime { get; set; }
            public string? DeleterId { get; set; }
        }

        private sealed class CreationOnlyEntity : IHasCreationTime
        {
            public DateTime CreationTime { get; set; }
        }

        private sealed class PlainEntity
        {
            public string Name { get; set; } = "plain";
        }

        private static AuditPropertySetter CreateSetter(out FixedClock clock)
        {
            clock = new FixedClock();
            return new AuditPropertySetter(clock, new FakeCurrentUser());
        }

        [Fact]
        public void SetCreationProperties_WritesTimeAndCreator()
        {
            var setter = CreateSetter(out var clock);
            var entity = new FullAuditedEntity();

            setter.SetCreationProperties(entity);

            Assert.Equal(clock.Now, entity.CreationTime);
            Assert.Equal("user-1", entity.CreatorId);
        }

        [Fact]
        public void SetCreationProperties_OnlyWritesImplementedInterfaces()
        {
            var setter = CreateSetter(out var clock);
            var entity = new CreationOnlyEntity();

            setter.SetCreationProperties(entity);

            Assert.Equal(clock.Now, entity.CreationTime);
        }

        [Fact]
        public void SetCreationProperties_IgnoresEntityWithoutAuditInterfaces()
        {
            var setter = CreateSetter(out _);
            var entity = new PlainEntity();

            setter.SetCreationProperties(entity);

            Assert.Equal("plain", entity.Name);
        }

        [Fact]
        public void SetModificationProperties_WritesTimeAndUser()
        {
            var setter = CreateSetter(out var clock);
            var entity = new FullAuditedEntity();

            setter.SetModificationProperties(entity);

            Assert.Equal(clock.Now, entity.LastModificationTime);
            Assert.Equal("user-1", entity.LastModifierId);
        }

        [Fact]
        public void SetDeletionProperties_MarksSoftDeleted()
        {
            var setter = CreateSetter(out var clock);
            var entity = new FullAuditedEntity();

            setter.SetDeletionProperties(entity);

            Assert.True(entity.IsDeleted);
            Assert.Equal(clock.Now, entity.DeletionTime);
            Assert.Equal("user-1", entity.DeleterId);
        }

        [Fact]
        public void Methods_ThrowOnNullEntity()
        {
            var setter = CreateSetter(out _);

            Assert.Throws<ArgumentNullException>(() => setter.SetCreationProperties(null!));
            Assert.Throws<ArgumentNullException>(() => setter.SetModificationProperties(null!));
            Assert.Throws<ArgumentNullException>(() => setter.SetDeletionProperties(null!));
        }

        [Fact]
        public void Ctor_ThrowsOnNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new AuditPropertySetter(null!, new FakeCurrentUser()));
            Assert.Throws<ArgumentNullException>(() => new AuditPropertySetter(new FixedClock(), null!));
        }
    }
}
