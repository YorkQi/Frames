using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frame.Core.Modules
{
    /// <summary>
    /// 把模块生命周期钩子挂到 Generic Host 的启动/停止流程上。
    /// 使用 <c>WebApplication</c> 或 <c>Host</c> 时由框架自动注册；只构建裸 <c>ServiceProvider</c> 时不会执行，
    /// 需要手动解析 <see cref="IModuleInitializer"/> 并调用。
    /// </summary>
    public sealed class ModuleInitializationHostedService : IHostedService
    {
        private readonly IModuleInitializer _initializer;
        private readonly ILogger<ModuleInitializationHostedService>? _logger;

        /// <summary>构造函数。</summary>
        /// <param name="initializer">模块初始化器</param>
        /// <param name="logger">日志（可选）</param>
        public ModuleInitializationHostedService(
            IModuleInitializer initializer,
            ILogger<ModuleInitializationHostedService>? logger = null)
        {
            _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("正在执行 Frame 模块初始化钩子…");
            await _initializer.InitializeAsync(cancellationToken);
            _logger?.LogInformation("Frame 模块初始化完成。");
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("正在执行 Frame 模块关闭钩子…");
            await _initializer.ShutdownAsync(cancellationToken);
        }
    }
}
