using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.EventBus
{
    public class LocalCacheEventFactory : IEventFactory
    {

        private static ConcurrentQueue<QueueEventOption> Events { get; set; } = new ConcurrentQueue<QueueEventOption>();

        /// <summary>
        /// 定时计划任务(0.5秒执行一次)
        /// </summary>
        readonly Timer threadTimer = new Timer(new TimerCallback(Exec), null, 0, 500);

        public async Task Push(IEvent @event)
        {
            await Task.Run(() =>
            {
                var eventType = @event.GetType();
                if (eventType.GetInterface("IEvent") == typeof(IEvent))
                {
                    Type[] gType = eventType.GenericTypeArguments;
                    var handler = string.Empty;
                    foreach (var item in gType)
                    {
                        if (item.GetInterface("IEventHandler") == typeof(IEventHandler))
                        {
                            handler = item.FullName;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(handler))
                    {
                        Events.Enqueue(new QueueEventOption
                        {
                            EventHandlerAssemblye = handler,
                            Param = @event
                        });
                    }
                }
            });
        }

        private static void Exec(object? state)
        {
            Task.Run(() =>
            {
                if (Events.TryDequeue(out QueueEventOption queue))
                {
                    var assembly = Assembly.GetEntryAssembly();
                    var type = assembly.GetType(queue.EventHandlerAssemblye); ;
                    if (type is null) throw new ApplicationException("Assmbly类型未找到");
                    MethodInfo? methodInfo = type.GetMethod("ExecuteAsync");
                    if (methodInfo is null) throw new ApplicationException("Assmbly类型ExecuteAsync方法未找到");
                    try
                    {
                        methodInfo.Invoke(Activator.CreateInstance(type), queue.Param is null ? new object[0] : new object[] { queue.Param });
                    }
                    catch (TargetInvocationException tie)
                    {
                        if (tie.InnerException is NotImplementedException)
                        {
                            throw new ApplicationException(tie.InnerException.Message, tie.InnerException);
                        }
                    }

                }
            });
        }

        ~LocalCacheEventFactory()
        {
            threadTimer.Dispose();
        }
    }
}
