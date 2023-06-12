using System.Threading.Tasks;

namespace Frame.EventBus
{
    /// <summary>
    /// 事件接口
    /// </summary>
    public interface IEventHandler
    {
        Task ExecuteAsync(IEvent param);
    }
}
