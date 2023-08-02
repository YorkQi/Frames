using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace Frame.EventBus
{
    public class LocalCacheEventBus : IEventBus
    {

        private static ConcurrentQueue<EventBusOption> Events { get; set; } = new ConcurrentQueue<EventBusOption>();
        private readonly IServiceProvider serviceProvider;
        private EventHandlerCollection? _eventHandlerCollection = new EventHandlerCollection();
        private Timer? timer;
        internal LocalCacheEventBus(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 初始化Event和EventHandler一对一映射
        /// </summary>
        /// <param name="eventHandlerCollection"></param>
        internal void Init(EventHandlerCollection eventHandlerCollection)
        {
            _eventHandlerCollection = eventHandlerCollection;
        }


        /// <summary>
        /// 发布事件订阅
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public async Task Push(IEvent @event)
        {
            await Task.Run(() =>
            {
                var eventType = @event.GetType();
                if (_eventHandlerCollection.Any(t => t.EnventType == eventType))
                {
                    var eventHandler = _eventHandlerCollection.FirstOrDefault(t => t.EnventType == eventType);
                    Events.Enqueue(new EventBusOption
                    {
                        EventHandlerType = eventHandler.EnventHandlerType,
                        Param = @event
                    });
                }
            });
        }


        internal void StartExec()
        {
            //设置定时间隔(毫秒为单位)
            int interval = 1000;
            timer = new Timer(interval)
            {
                //设置执行一次（false）还是一直执行(true)
                AutoReset = true,
                //设置是否执行System.Timers.Timer.Elapsed事件
                Enabled = true
            };
            //绑定Elapsed事件
            timer.Elapsed += new ElapsedEventHandler(Exec);

        }

        /// <summary>
        /// 执行事件订阅
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        private void Exec(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                if (Events.TryDequeue(out EventBusOption queue))
                {
                    if (queue.EventHandlerType is null) throw new ApplicationException("类型未找到");
                    if (queue.Param is null) throw new ApplicationException("事件没有参数");

                    var eventHandler = serviceProvider.GetService(queue.EventHandlerType);
                    string methodName = nameof(IEventHandler<IEvent>.ExecuteAsync);
                    MethodInfo? method = queue.EventHandlerType.GetMethod(methodName);
                    if (method != null)
                    {
                        try
                        {
                            object? result = method.Invoke(eventHandler, new[] { queue.Param });
                            ((Task)result!).Wait();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }

            });
        }
    }
}
