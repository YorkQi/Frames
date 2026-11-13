using Frame.Scheduler;
using Xunit;

namespace TestUnit
{
    public class JobScheduleTest
    {
        [Fact]
        public void Ctor_WithAllParameters()
        {
            var desc = new JobSchedule("MyJobName", "MyJob", "0/5 * * * * *", "Test job");

            Assert.Equal("MyJobName", desc.JobName);
            Assert.Equal("MyJob", desc.FullName);
            Assert.Equal("0/5 * * * * *", desc.CronExpression);
            Assert.Equal("Test job", desc.Description);
        }

        [Fact]
        public void JobName_Required()
        {
            Assert.Throws<ArgumentNullException>(() => new JobSchedule(null!, "MyJob", "* * * * *"));
        }
    }
}
