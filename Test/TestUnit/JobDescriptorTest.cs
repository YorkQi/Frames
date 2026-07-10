using Frame.Scheduler;
using Xunit;

namespace TestUnit
{
    public class JobScheduleTest
    {
        [Fact]
        public void Ctor_WithAllParameters()
        {
            var desc = new JobSchedule("MyJob", "0/5 * * * * ?", "Test job");

            Assert.Equal("MyJob", desc.FullName);
            Assert.Equal("0/5 * * * * ?", desc.CronExpression);
            Assert.Equal("Test job", desc.Description);
        }

        [Fact]
        public void Description_DefaultsToNull()
        {
            var desc = new JobSchedule("Job", "* * * * *");
            Assert.Equal("Job", desc.FullName);
            Assert.Equal("* * * * *", desc.CronExpression);
            Assert.Null(desc.Description);
        }
    }
}
