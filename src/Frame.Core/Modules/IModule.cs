using Microsoft.Extensions.DependencyInjection;

namespace Frame.Core.FrameModules
{
    public interface IModule
    {
        void Load(IServiceCollection services);
    }
}
