using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Modules
{
    /// <summary>
    /// 模块基类，提供 <see cref="IFrameModule"/> 的默认空实现。
    /// 用户模块只需继承此类并重写需要的方法。
    /// </summary>
    public abstract class FrameModule : IFrameModule
    {
        /// <summary>配置服务前（默认无操作）</summary>
        public virtual void PreConfigureServices(ServiceConfigurationContext context) { }

        /// <summary>配置服务（默认无操作）</summary>
        public virtual void ConfigureServices(ServiceConfigurationContext context) { }

        /// <summary>配置服务后（默认无操作）</summary>
        public virtual void PostConfigureServices(ServiceConfigurationContext context) { }

        /// <summary>应用初始化（默认无操作）</summary>
        public virtual Task OnApplicationInitializationAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        /// <summary>应用关闭（默认无操作）</summary>
        public virtual Task OnApplicationShutdownAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
