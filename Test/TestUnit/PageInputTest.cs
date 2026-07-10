using Frame.Core.Entities.Dtos;
using Xunit;

namespace TestUnit
{
    public class PageInputTest
    {
        [Fact]
        public void Defaults_AreCorrect()
        {
            var input = new PageInput();
            Assert.Equal(1, input.Page);
            Assert.Equal(20, input.Limit);
        }

        [Fact]
        public void CanOverrideDefaults()
        {
            var input = new PageInput { Page = 3, Limit = 50 };
            Assert.Equal(3, input.Page);
            Assert.Equal(50, input.Limit);
        }
    }
}
