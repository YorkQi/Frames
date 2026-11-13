using System.Linq;
using System.Reflection;
using Frame.Core.Uow;
using Frame.Core.Uow.Interception;
using Microsoft.Extensions.DependencyInjection;


namespace Frame.Core.Application
{
    /// <summary>
    /// 应用服务自动注册。
    /// </summary>
    public static class ServiceApplicationConfiguration
    {
        /// <summary>
        /// 扫描并注册所有实现 <see cref="IApplicationService"/> 的服务。
        /// 标注了 <see cref="UnitOfWorkAttribute"/>（类级或任一 public 实例方法级）的服务会用
        /// <see cref="UnitOfWorkProxy"/> 包装，从而获得自动工作单元/事务能力。
        /// </summary>
        /// <param name="configuration">服务配置上下文</param>
        /// <returns>服务配置上下文，用于链式调用</returns>
        public static ServiceConfigurationContext UseApplication(this ServiceConfigurationContext configuration)
        {
            var assemblyTypes = configuration.GetAssemblyTypes();
            var targetInterface = typeof(IApplicationService);
            foreach (var type in assemblyTypes)
            {
                if (!type.IsPublic || !type.IsClass || type.IsAbstract)
                    continue;

                var serviceInterface = type.GetInterfaces()
                    .FirstOrDefault(t => t != targetInterface && targetInterface.IsAssignableFrom(t));
                if (serviceInterface is null)
                    continue;

                if (RequiresUnitOfWork(type, serviceInterface))
                {
                    // 用 DispatchProxy 包装：方法调用前后自动开启/提交/回滚工作单元
                    configuration.Add(ServiceDescriptor.Scoped(serviceInterface, sp => UnitOfWorkProxy.Create(
                        serviceInterface,
                        ActivatorUtilities.CreateInstance(sp, type),
                        sp.GetRequiredService<IUnitOfWorkManager>())));
                }
                else
                {
                    configuration.Add(ServiceDescriptor.Scoped(serviceInterface, type));
                }
            }
            return configuration;
        }

        private static bool RequiresUnitOfWork(System.Type type, System.Type serviceInterface)
        {
            // 类级、实现类的 public 实例方法级、以及接口方法级都算
            if (type.IsDefined(typeof(UnitOfWorkAttribute), inherit: true))
                return true;

            if (serviceInterface.IsDefined(typeof(UnitOfWorkAttribute), inherit: true))
                return true;

            if (type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(m => m.IsDefined(typeof(UnitOfWorkAttribute), inherit: true)))
                return true;

            return serviceInterface.GetMethods()
                .Any(m => m.IsDefined(typeof(UnitOfWorkAttribute), inherit: true));
        }
    }
}
