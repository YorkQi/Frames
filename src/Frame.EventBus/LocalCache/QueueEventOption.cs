using System;

namespace Frame.EventBus
{
    public class QueueEventOption
    {
        /// <summary>
        /// 事件编号
        /// </summary>
        public Guid EventId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// IEventHandler事件类型
        /// </summary>
        public string EventHandlerAssemblye { get; set; } = string.Empty;

        /// <summary>
        /// 执行事件使用参数
        /// </summary>
        public IEvent? Param { get; set; }
    }
}
