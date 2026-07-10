using System.Threading.Tasks;

namespace Frame.EventBus
{
    /// <summary>
    /// 事件接口
    /// </summary>
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        Task ExecuteAsync(TEvent param);
    }
}
