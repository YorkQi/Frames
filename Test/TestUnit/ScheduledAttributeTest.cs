using Frame.Scheduler;
using Xunit;

namespace TestUnit
{
    public class ScheduledAttributeTest
    {
        [Fact]
        public void Ctor_SetsCronExpression()
        {
            var attr = new ScheduledAttribute("0/5 * * * * ?");
            Assert.Equal("0/5 * * * * ?", attr.CronExpression);
        }

        [Fact]
        public void Description_DefaultsToNull()
        {
            var attr = new ScheduledAttribute("*/10 * * * *");
            Assert.Null(attr.Description);
        }
    }
}
