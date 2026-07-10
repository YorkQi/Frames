using Frame.Locks;
using Xunit;

namespace TestUnit
{
    public class LockOptionsTest
    {
        [Fact]
        public void Ctor_StoresConnections()
        {
            var connections = new[] { "redis1", "redis2" };
            var options = new LockOptions(connections);

            Assert.Equal(connections, options.Connections);
        }

        [Fact]
        public void Connections_CanBeUpdated()
        {
            var options = new LockOptions(new[] { "old" });
            options.Connections = new[] { "new1", "new2" };

            Assert.Equal(2, options.Connections.Count());
        }

        [Fact]
        public void Ctor_WithNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new LockOptions(null!));
        }
    }
}
