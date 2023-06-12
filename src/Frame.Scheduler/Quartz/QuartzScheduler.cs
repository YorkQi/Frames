using Quartz;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Frame.Scheduler
{
    public class QuartzScheduler : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
             {
                 var schedulerAssmbly = context.MergedJobDataMap.Get("SchedulerAssmbly").ToString();
                 var assembly = Assembly.GetEntryAssembly();
                 var type = assembly.GetType(schedulerAssmbly);
                 if (type is null) throw new ArgumentNullException(nameof(type));

                 MethodInfo? methodInfo = type.GetMethod("ExecuteAsync");
                 if (methodInfo is null) throw new ApplicationException("SchedulerAssmbly类型ExecuteAsync方法未找到");
                 try
                 {
                     methodInfo.Invoke(Activator.CreateInstance(type), new object[0]);
                 }
                 catch (TargetInvocationException tie)
                 {
                     if (tie.InnerException is NotImplementedException)
                     {
                         throw new ApplicationException(tie.InnerException.Message, tie.InnerException);
                     }
                 }
             });
        }

    }
}
