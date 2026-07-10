using Frame.Core.Utils;
using Xunit;

namespace TestUnit
{
    public class FrameCheckTest
    {
        [Fact]
        public void NotNull_WithNonNullValue_ReturnsSameValue()
        {
            var obj = new object();
            var result = Check.NotNull(obj, nameof(obj));
            Assert.Same(obj, result);
        }

        [Fact]
        public void NotNull_WithNullValue_ThrowsArgumentNullException()
        {
            object? obj = null;
            var ex = Assert.Throws<ArgumentNullException>(() => Check.NotNull(obj, nameof(obj)));
            Assert.Equal("obj", ex.ParamName);
        }

        [Fact]
        public void NotNull_WithCustomMessage_IncludesMessage()
        {
            object? obj = null;
            var ex = Assert.Throws<ArgumentNullException>(() => Check.NotNull(obj, "param", "custom message"));
            Assert.Contains("custom message", ex.Message);
        }

        [Fact]
        public void NotNull_WithNullableValueType_Works()
        {
            int? value = 42;
            var result = Check.NotNull(value, nameof(value));
            Assert.Equal(42, result);
        }

        [Fact]
        public void NotNull_WithNullableValueTypeNull_Throws()
        {
            int? value = null;
            Assert.Throws<ArgumentNullException>(() => Check.NotNull(value, nameof(value)));
        }
    }
}
