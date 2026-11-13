using Frame.Scheduler;
using Xunit;

namespace TestUnit
{
    public class ScheduledAttributeTest
    {
        [Fact]
        public void Ctor_SetsJobNameAndCronExpression()
        {
            var attr = new ScheduledAttribute("MyJob", "0/5 * * * * *");
            Assert.Equal("MyJob", attr.JobName);
            Assert.Equal("0/5 * * * * *", attr.CronExpression);
        }

        [Fact]
        public void Description_DefaultsToNull()
        {
            var attr = new ScheduledAttribute("MyJob", "*/10 * * * *");
            Assert.Equal("MyJob", attr.JobName);
            Assert.Null(attr.Description);
        }

        [Fact]
        public void JobName_Required()
        {
            Assert.Throws<ArgumentNullException>(() => new ScheduledAttribute(null!, "* * * * *"));
        }
    }
}
