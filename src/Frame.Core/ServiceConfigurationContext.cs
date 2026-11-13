using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Core
{
    /// <summary>
    /// 框架服务配置上下文。
    /// 负责管理服务注册、程序集类型扫描和模块生命周期。
    /// 是框架所有扩展方法（UseApplication、UseModule、UseDatabaseContext 等）的共同入口。
    /// </summary>
    public class ServiceConfigurationContext
    {
        private readonly List<ServiceDescriptor> _serviceDescriptors = [];
        private readonly HashSet<Assembly> _assemblies = [];
        private IReadOnlyList<Type>? _allTypes; // 惰性缓存，按需加载所有注册程序集的类型

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="assemblies">初始程序集集合。如果为 null，则使用默认全量扫描作为兜底。</param>
        internal ServiceConfigurationContext(IEnumerable<Assembly>? assemblies = null)
        {
            if (assemblies is not null)
            {
                foreach (var asm in assemblies)
                {
                    _assemblies.Add(asm);
                }
            }
        }

        /// <summary>
        /// 注册一个需要被框架扫描的程序集。
        /// 框架将扫描此程序集中标注了 <see cref="Modules.ServiceModuleAttribute"/>、
        /// 实现了 <see cref="Repositories.IRepository"/> 或 <see cref="Application.IApplicationService"/>
        /// 的类型并自动注册。
        /// </summary>
        /// <param name="assembly">要扫描的程序集</param>
        /// <returns>当前上下文，支持链式调用</returns>
        public ServiceConfigurationContext AddAssembly(Assembly assembly)
        {
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));
            _assemblies.Add(assembly);
            _allTypes = null; // 清除缓存，下次访问时重建
            return this;
        }

        /// <summary>
        /// 通过程序集中的某个类型来注册该程序集。方便在 fluent 链中使用。
        /// </summary>
        /// <typeparam name="T">程序集中的任意类型</typeparam>
        public ServiceConfigurationContext AddAssemblyOf<T>()
        {
            return AddAssembly(typeof(T).Assembly);
        }

        /// <summary>
        /// 获取所有已注册程序集中的公开导出类型。
        /// 结果会被缓存，调用 <see cref="AddAssembly"/> 后会清除缓存。
        /// </summary>
        public IReadOnlyList<Type> GetAssemblyTypes()
        {
            if (_allTypes is not null)
                return _allTypes;
            var types = new List<Type>();
            // 有显式注册的程序集 → 仅扫描这些
            if (_assemblies.Count > 0)
            {
                foreach (var asm in _assemblies)
                {
                    try
                    {
                        types.AddRange(asm.GetExportedTypes());
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // 部分类型加载失败时尽量加载已成功的
                        types.AddRange(ex.Types.Where(t => t is not null)!);
                    }
                }
            }
            _allTypes = types;
            return _allTypes;
        }

        /// <summary>获取指定完全限定名的类型（在所有已注册程序集中查找）</summary>
        public Type? GetType(string typeFullName)
        {
            return GetAssemblyTypes().FirstOrDefault(t => t.FullName == typeFullName);
        }


        /// <summary>
        /// 获取当前上下文中已注册的所有服务描述符列表。
        /// 供框架内部在最终构建 DI 容器时使用。
        /// </summary>
        internal List<ServiceDescriptor> GetServiceDescriptors()
        {
            return _serviceDescriptors;
        }

        /// <summary>
        /// 添加一个服务注册。
        /// </summary>
        /// <param name="service">服务描述符（由 Microsoft.Extensions.DI 定义）</param>
        public void Add(ServiceDescriptor service)
        {
            if (service is null) throw new ArgumentNullException(nameof(service));
            _serviceDescriptors.Add(service);
        }

        /// <summary>
        /// 批量添加多个服务注册。
        /// </summary>
        public void AddRange(IEnumerable<ServiceDescriptor> services)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            _serviceDescriptors.AddRange(services);
        }

        /// <summary>
        /// 移除符合条件的所有服务注册。可用于替换框架的默认实现。
        /// </summary>
        /// <param name="predicate">移除条件</param>
        public void RemoveService(Func<ServiceDescriptor, bool> predicate)
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));
            _serviceDescriptors.RemoveAll(new Predicate<ServiceDescriptor>(predicate));
        }
    }
}
