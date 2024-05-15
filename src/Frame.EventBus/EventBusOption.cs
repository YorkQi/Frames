using System;
using System.ComponentModel.DataAnnotations;

namespace Frame.EventBus
{
    internal class EventBusOption
    {
        /// <summary>
        /// IEventHandler事件类型
        /// </summary>
        [Required(ErrorMessage = "请设置事件处理程序类型")]
        public Type? EventHandlerType { get; set; }
        /// <summary>
        /// 执行事件使用参数
        /// </summary>
        [Required(ErrorMessage = "请设置事件参数")]
        public IEvent? Param { get; set; }
    }
}
