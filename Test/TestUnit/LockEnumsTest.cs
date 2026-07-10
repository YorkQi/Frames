using Frame.Locks.Enums;
using Xunit;

namespace TestUnit
{
    public class LockEnumsTest
    {
        [Fact]
        public void LockState_HasExpectedValues()
        {
            Assert.Equal(0, (int)LockState.Unlocked);
            Assert.Equal(1, (int)LockState.Acquired);
            Assert.Equal(2, (int)LockState.NoQuorum);
            Assert.Equal(3, (int)LockState.Conflicted);
            Assert.Equal(4, (int)LockState.Expired);
        }

        [Fact]
        public void LockType_HasExpectedValues()
        {
            Assert.Equal(0, (int)LockType.Local);
            Assert.Equal(1, (int)LockType.Redis);
        }
    }
}
