using System.Threading.Tasks;

namespace Frame.EventBus
{

    public interface IEventFactory
    {

        Task Push(IEvent @event);

    }
}
