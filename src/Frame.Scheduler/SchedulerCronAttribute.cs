using System;

namespace Frame.Scheduler
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SchedulerCronAttribute : Attribute
    {
        public string Cron { get; private set; }

        public SchedulerCronAttribute(string cron)
        {
            Cron = cron;
        }
    }
}
