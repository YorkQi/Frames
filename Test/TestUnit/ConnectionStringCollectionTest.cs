using Frame.Databases;
using Frame.Databases.Enums;
using Xunit;

namespace TestUnit
{
    public class ConnectionStringCollectionTest
    {
        [Fact]
        public void Ctor_WithParams_StoresConnections()
        {
            var collection = new ConnectionStringCollection("a", "b", "c");
            Assert.Equal(3, collection.ToArray().Length);
        }

        [Fact]
        public void AddRange_AppendsConnections()
        {
            var collection = new ConnectionStringCollection("a");
            collection.AddRange(["b", "c"]);
            Assert.Equal(3, collection.ToArray().Length);
        }

        private static readonly string[] collection0 = ["a", "b", "c"];

        [Fact]
        public void GetConnection_Random_ReturnsValidConnection()
        {
            var collection = new ConnectionStringCollection("a", "b", "c");
            var result = collection.GetConnection(ConnectionStringStrategy.Random);
            Assert.Contains(result, collection0);
        }

        [Fact]
        public void GetConnection_RoundRobin_CyclesThrough()
        {
            var collection = new ConnectionStringCollection("a", "b", "c");

            var results = new HashSet<string>();
            for (int i = 0; i < 30; i++)
            {
                results.Add(collection.GetConnection(ConnectionStringStrategy.RoundRobin));
            }

            Assert.Equal(3, results.Count);
            Assert.Contains("a", results);
            Assert.Contains("b", results);
            Assert.Contains("c", results);
        }

        [Fact]
        public void GetConnection_EmptyCollection_Throws()
        {
            var collection = new ConnectionStringCollection();
            Assert.Throws<InvalidOperationException>(() =>
                collection.GetConnection(ConnectionStringStrategy.Random));
        }

        [Fact]
        public void ToArray_ReturnsCopy()
        {
            var collection = new ConnectionStringCollection("a", "b");
            var arr = collection.ToArray();
            Assert.Equal(2, arr.Length);
            Assert.Contains("a", arr);
            Assert.Contains("b", arr);
        }

        [Fact]
        public void Enumerator_IteratesAll()
        {
            var collection = new ConnectionStringCollection("a", "b");
            var count = 0;
            foreach (var _ in collection)
                count++;
            Assert.Equal(2, count);
        }
    }
}
