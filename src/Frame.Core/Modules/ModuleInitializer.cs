using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Modules
{
    /// <summary>
    /// 模块初始化器。按拓扑排序后的顺序调用模块的应用初始化/关闭钩子。
    /// 由框架注册为 Singleton，并由 <see cref="ModuleInitializationHostedService"/> 在 Host 启动/停止时调用；
    /// 不使用 Host 的场景可以手动解析并调用。
    /// </summary>
    public interface IModuleInitializer
    {
        /// <summary>应用初始化（按模块依赖顺序，父模块先执行）。</summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>应用关闭（逆序，子模块先执行）。</summary>
        Task ShutdownAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>模块初始化器默认实现。</summary>
    public sealed class ModuleInitializer : IModuleInitializer
    {
        private readonly IReadOnlyList<IFrameModule> _modules;

        /// <summary>构造函数。</summary>
        /// <param name="modules">已按依赖排序的模块实例</param>
        public ModuleInitializer(IReadOnlyList<IFrameModule> modules)
        {
            _modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            foreach (var module in _modules)
            {
                await module.OnApplicationInitializationAsync(cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            // 逆序关闭（与初始化顺序相反）
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                await _modules[i].OnApplicationShutdownAsync(cancellationToken);
            }
        }
    }
}
