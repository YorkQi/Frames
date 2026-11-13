using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Modules
{
    /// <summary>
    /// 模块接口。定义了模块的完整生命周期方法。
    /// 实现此接口的类将被框架识别为可初始化模块。
    /// 生命周期顺序：
    /// 1. <see cref="PreConfigureServices"/> — 配置服务前（可用于替换默认注册）
    /// 2. <see cref="ConfigureServices"/> — 配置服务（主要注册入口）
    /// 3. <see cref="PostConfigureServices"/> — 配置服务后（可用于修改已注册的服务）
    /// 4. <see cref="OnApplicationInitializationAsync"/> — 应用初始化（启动时执行）
    /// 5. <see cref="OnApplicationShutdownAsync"/> — 应用关闭（优雅关闭）
    /// </summary>
    public interface IFrameModule
    {
        /// <summary>
        /// 配置服务前阶段。
        /// 在 ConfigureServices 之前调用，可用于替换或修改框架的默认注册。
        /// 此阶段不应依赖其他模块的注册结果。
        /// </summary>
        void PreConfigureServices(ServiceConfigurationContext context);

        /// <summary>
        /// 配置服务阶段。
        /// 在此方法中注册当前模块所需的依赖服务。
        /// 这是模块注册服务的主要入口。
        /// </summary>
        void ConfigureServices(ServiceConfigurationContext context);

        /// <summary>
        /// 配置服务后阶段。
        /// 在 ConfigureServices 之后调用，此时所有模块的服务均已注册。
        /// 可用于修改依赖模块的注册，或执行依赖检查。
        /// </summary>
        void PostConfigureServices(ServiceConfigurationContext context);

        /// <summary>
        /// 应用初始化阶段。
        /// 在应用启动时调用，此时所有服务已注册完毕。
        /// 可用于预加载缓存、检查数据库连接、启动后台服务等。
        /// </summary>
        Task OnApplicationInitializationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 应用关闭阶段。
        /// 在应用优雅关闭时调用。可用于释放非托管资源、等待任务完成等。
        /// </summary>
        Task OnApplicationShutdownAsync(CancellationToken cancellationToken = default);
    }
}
