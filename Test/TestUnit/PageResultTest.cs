using Frame.Core.Entities.Dtos;
using Xunit;

namespace TestUnit
{
    public class PageResultTest
    {
        [Fact]
        public void Ctor_SetsProperties()
        {
            var items = new[] { "a", "b" };
            var result = new PageResult<string>(items, 10);

            Assert.Equal(items, result.Items);
            Assert.Equal(10, result.Count);
        }

        [Fact]
        public void EmptyItems_Works()
        {
            var items = Array.Empty<string>();
            var result = new PageResult<string>(items, 0);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.Count);
        }
    }
}
