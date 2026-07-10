using Frame.Core.Entities;
using Xunit;

namespace TestUnit
{
    public class EntityTest
    {
        private class TestEntity : Entity<int> { }
        private class AnotherEntity : Entity<int> { }

        [Fact]
        public void SameId_AreEqual()
        {
            var a = new TestEntity { Id = 1 };
            var b = new TestEntity { Id = 1 };
            Assert.True(a.Equals(b));
            Assert.True(a == b);
        }

        [Fact]
        public void DifferentId_AreNotEqual()
        {
            var a = new TestEntity { Id = 1 };
            var b = new TestEntity { Id = 2 };
            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void SameReference_AreEqual()
        {
            var a = new TestEntity { Id = 1 };
            Assert.True(a.Equals(a));
        }

        [Fact]
        public void NullComparison_AreNotEqual()
        {
            var a = new TestEntity { Id = 1 };
            Assert.False(a.Equals(null));
        }

        [Fact]
        public void BothNull_AreEqual()
        {
            TestEntity? a = null;
            TestEntity? b = null;
#pragma warning disable CS8604
            Assert.True(a == b);
#pragma warning restore CS8604
        }

        [Fact]
        public void DifferentTypes_AreNotEqual()
        {
            var a = new TestEntity { Id = 1 };
            var b = new AnotherEntity { Id = 1 };
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void WithNonEntity_ReturnsFalse()
        {
            var a = new TestEntity { Id = 1 };
            var obj = new object();
            Assert.False(a.Equals(obj));
        }
    }
}
