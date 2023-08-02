using System.Threading.Tasks;

namespace Frame.EventBus
{

    public interface IEventBus
    {

        Task Push(IEvent @event);

    }
}
