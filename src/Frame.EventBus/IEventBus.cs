using System.Threading.Tasks;

namespace Frame.EventBus
{
    public partial interface IEventBus
    {
        Task Push(IEvent @event);
    }
}
