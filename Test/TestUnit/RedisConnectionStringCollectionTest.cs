using Frame.Redis;
using Frame.Redis.Enums;
using Xunit;

namespace TestUnit
{
    public class RedisConnectionStringCollectionTest
    {
        [Fact]
        public void Ctor_WithParams_StoresConnections()
        {
            var collection = new RedisConnectionStringCollection("a", "b");
            Assert.Equal(2, collection.Get().Count());
        }

        [Fact]
        public void AddRange_AppendsConnections()
        {
            var collection = new RedisConnectionStringCollection("a");
            collection.AddRange(new[] { "b", "c" });
            Assert.Equal(3, collection.Get().Count());
        }

        [Fact]
        public void GetConnection_Parameterless_DefaultsToRoundRobin()
        {
            var collection = new RedisConnectionStringCollection("a", "b");
            var results = new HashSet<string>();
            for (int i = 0; i < 20; i++)
                results.Add(collection.GetConnection());
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void GetConnection_Random_ReturnsValidConnection()
        {
            var collection = new RedisConnectionStringCollection("a", "b", "c");
            var result = collection.GetConnection(ConnectionStringStrategy.Random);
            Assert.Contains(result, new[] { "a", "b", "c" });
        }

        [Fact]
        public void GetConnection_RoundRobin_CyclesThrough()
        {
            var collection = new RedisConnectionStringCollection("a", "b", "c");
            var results = new HashSet<string>();
            for (int i = 0; i < 30; i++)
                results.Add(collection.GetConnection(ConnectionStringStrategy.RoundRobin));
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void GetConnection_EmptyCollection_Throws()
        {
            var collection = new RedisConnectionStringCollection();
            Assert.Throws<InvalidOperationException>(() =>
                collection.GetConnection(ConnectionStringStrategy.Random));
        }

        [Fact]
        public void Add_Params_AppendsMultiple()
        {
            var collection = new RedisConnectionStringCollection();
            collection.Add("a", "b", "c");
            Assert.Equal(3, collection.Get().Count());
        }

        [Fact]
        public void Enumerator_IteratesAll()
        {
            var collection = new RedisConnectionStringCollection("a", "b");
            var count = 0;
            foreach (var _ in collection)
                count++;
            Assert.Equal(2, count);
        }
    }
}
