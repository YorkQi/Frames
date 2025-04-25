using System.Threading.Tasks;

namespace Frame.EventBus
{
    public partial interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        Task Push(IEvent @event);
    }
}
