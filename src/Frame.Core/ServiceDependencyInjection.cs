using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Frame.Core
{
    /// <summary>
    /// Frame 框架的 DI 注册入口。
    /// 提供 <c>IServiceCollection.AddFrameService()</c> 扩展方法，用于初始化框架。
    /// </summary>
    public static class ServiceDependencyInjection
    {
        /// <summary>
        /// 注册 Frame 框架所需的所有服务。
        /// </summary>
        /// <param name="services">DI 容器</param>
        /// <param name="configuration">框架配置回调。在此回调中注册程序集和启用框架功能模块。</param>
        /// <returns>服务集合，用于链式调用</returns>
        public static IServiceCollection AddFrameService([NotNull] this IServiceCollection services,
            Action<ServiceConfigurationContext>? configuration = null)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            // 【优化】自动发现所有被引用的业务程序集，而非仅扫描入口程序集
            var assemblies = GetApplicationAssemblies();
            var frameConfiguration = new ServiceConfigurationContext(assemblies);

            // 阶段1：预先配置（允许模块在 ConfigureServices 之前做准备工作）
            configuration?.Invoke(frameConfiguration);

            // 阶段2：注册模块并执行模块生命周期
            RegisterModules(frameConfiguration);

            // 阶段3：将所有注册的服务描述符注入 DI 容器
            services.Add(frameConfiguration.GetServiceDescriptors());

            // 阶段4：注入框架基础设施（UOW 管理器等）
            RegisterInfrastructure(services, frameConfiguration);

            return services;
        }

        /// <summary>
        /// 注册模块，并按照依赖拓扑顺序执行生命周期方法。
        /// </summary>
        private static void RegisterModules(ServiceConfigurationContext context)
        {
            var moduleTypes = context.GetAssemblyTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IFrameModule).IsAssignableFrom(t))
                .ToList();

            if (moduleTypes.Count == 0)
                return;

            // 按依赖拓扑排序
            var sorted = SortModulesByDependency(moduleTypes);

            // PreConfigureServices（正序——父模块先执行）
            foreach (var moduleType in sorted)
            {
                if (Activator.CreateInstance(moduleType) is IFrameModule module)
                {
                    module.PreConfigureServices(context);
                    // 将模块实例注册为 Singleton，后续初始化阶段可复用
                    context.Add(ServiceDescriptor.Singleton(moduleType, module));
                }
            }

            // ConfigureServices（正序）
            foreach (var moduleType in sorted)
            {
                if (context.GetServiceDescriptors().Any(d => d.ServiceType == moduleType))
                {
                    var module = (IFrameModule)context.GetServiceDescriptors()
                        .First(d => d.ServiceType == moduleType).ImplementationInstance!;
                    module.ConfigureServices(context);
                }
            }

            // PostConfigureServices（逆序——子模块先执行，确保子模块配置在父模块之后）
            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                var moduleType = sorted[i];
                if (context.GetServiceDescriptors().Any(d => d.ServiceType == moduleType))
                {
                    var module = (IFrameModule)context.GetServiceDescriptors()
                        .First(d => d.ServiceType == moduleType).ImplementationInstance!;
                    module.PostConfigureServices(context);
                }
            }
        }

        /// <summary>
        /// 按依赖拓扑对模块进行拓扑排序。
        /// 确保依赖模块（父模块）在依赖它的模块（子模块）之前初始化。
        /// 参考 ABP 的模块依赖排序算法。
        /// </summary>
        private static List<Type> SortModulesByDependency(List<Type> moduleTypes)
        {
            var visited = new HashSet<Type>();
            var sorted = new List<Type>();

            void DfsVisit(Type type)
            {
                if (visited.Contains(type))
                    return;

                visited.Add(type);

                var dependsOnAttrs = type.GetCustomAttributes<DependsOnAttribute>();
                foreach (var attr in dependsOnAttrs)
                {
                    foreach (var dep in attr.DependedModuleTypes)
                    {
                        if (moduleTypes.Contains(dep))
                        {
                            DfsVisit(dep);
                        }
                    }
                }

                sorted.Add(type);
            }

            foreach (var t in moduleTypes)
            {
                DfsVisit(t);
            }

            return sorted;
        }

        /// <summary>
        /// 自动发现所有被入口程序集引用的业务程序集。
        /// </summary>
        private static Assembly[] GetApplicationAssemblies()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is null)
                return [];

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<Assembly>();
            var queue = new Queue<Assembly>();

            queue.Enqueue(entryAssembly);
            var entryName = entryAssembly.GetName().Name!;
            visited.Add(entryName);

            while (queue.Count > 0)
            {
                var assembly = queue.Dequeue();
                result.Add(assembly);

                foreach (var referencedName in assembly.GetReferencedAssemblies())
                {
                    var name = referencedName.Name!;

                    if (!visited.Add(name))
                        continue;

                    // 跳过 .NET 运行时程序集和框架原生库
                    if (IsFrameworkAssembly(name))
                        continue;

                    try
                    {
                        queue.Enqueue(Assembly.Load(referencedName));
                    }
                    catch
                    {
                        // 单个程序集加载失败不应阻止整体初始化
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 判断是否为 .NET 运行时 / 框架程序集，跳过这些无需扫描。
        /// </summary>
        private static bool IsFrameworkAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 注册框架基础设施服务。
        /// 这些是框架运行所必须的基础服务。
        /// </summary>
        private static void RegisterInfrastructure(IServiceCollection services, ServiceConfigurationContext context)
        {
            // 注册 UOW 管理器（如果尚未注册）
            services.TryAddScoped<Uow.IUnitOfWorkManager, Uow.UnitOfWorkManager>();

            // 注册模块初始化器（如果存在模块的话）
            var moduleTypes = context.GetAssemblyTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IFrameModule).IsAssignableFrom(t))
                .ToList();

            if (moduleTypes.Count > 0)
            {
                services.AddSingleton<IModuleInitializer>(sp =>
                {
                    var modules = moduleTypes
                        .Select(t => sp.GetRequiredService(t) as IFrameModule)
                        .Where(m => m is not null)
                        .Cast<IFrameModule>()
                        .ToList();
                    return new ModuleInitializer(modules);
                });
            }
        }
    }

    /// <summary>
    /// 模块初始化器，在应用启动时和关闭时调用模块的生命周期方法。
    /// 由框架自动注入为 Singleton，在 Host 启动时由 <c>IHostedService</c> 调用。
    /// </summary>
    internal interface IModuleInitializer
    {
        /// <summary>应用初始化</summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>应用关闭</summary>
        Task ShutdownAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>模块初始化器默认实现</summary>
    internal sealed class ModuleInitializer(IReadOnlyList<IFrameModule> modules) : IModuleInitializer
    {
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            foreach (var module in modules)
            {
                await module.OnApplicationInitializationAsync(cancellationToken);
            }
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            // 逆序关闭（与初始化顺序相反）
            for (int i = modules.Count - 1; i >= 0; i--)
            {
                await modules[i].OnApplicationShutdownAsync(cancellationToken);
            }
        }
    }
}
