using Microsoft.Extensions.DependencyInjection;

namespace Frame.Core.AutoInjections
{
    public interface IModule
    {
        void Load(IServiceCollection services);
    }
}
