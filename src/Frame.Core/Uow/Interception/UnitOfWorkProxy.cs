using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Uow.Interception
{
    /// <summary>
    /// 工作单元代理工厂。
    /// 通过 <see cref="DispatchProxy"/> 为目标接口生成代理，使标注了
    /// <see cref="UnitOfWorkAttribute"/> 的方法自动获得「开启 UOW → 调用 → 提交/回滚」的行为。
    /// </summary>
    /// <remarks>
    /// 代理只支持接口类型。<c>UseApplication()</c> 在扫描到带 <see cref="UnitOfWorkAttribute"/>
    /// 的应用服务时会自动使用本工厂创建代理。
    /// </remarks>
    public static class UnitOfWorkProxy
    {
        /// <summary>
        /// 为接口 <typeparamref name="T"/> 创建带工作单元拦截的代理。
        /// </summary>
        /// <typeparam name="T">接口类型</typeparam>
        /// <param name="target">真实实现</param>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        /// <returns>代理实例</returns>
        public static T Create<T>(T target, IUnitOfWorkManager unitOfWorkManager) where T : class
        {
            if (target is null) throw new ArgumentNullException(nameof(target));
            if (unitOfWorkManager is null) throw new ArgumentNullException(nameof(unitOfWorkManager));
            if (!typeof(T).IsInterface)
                throw new ArgumentException("工作单元代理只能用于接口类型。", nameof(T));

            var proxy = DispatchProxy.Create<T, UnitOfWorkInterceptor>();
            ((UnitOfWorkInterceptor)(object)proxy).SetTarget(target, unitOfWorkManager);
            return proxy;
        }

        /// <summary>
        /// 非泛型版本，供 DI 工厂在运行期使用。
        /// </summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="target">真实实现</param>
        /// <param name="unitOfWorkManager">工作单元管理器</param>
        /// <returns>代理实例（需强转为 <paramref name="serviceType"/>）</returns>
        public static object Create(Type serviceType, object target, IUnitOfWorkManager unitOfWorkManager)
        {
            if (serviceType is null) throw new ArgumentNullException(nameof(serviceType));

            var method = typeof(UnitOfWorkProxy)
                .GetMethod(nameof(CreateGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(serviceType);
            return method.Invoke(null, new[] { target, unitOfWorkManager })!;
        }

        private static T CreateGeneric<T>(object target, IUnitOfWorkManager unitOfWorkManager) where T : class
            => Create((T)target, unitOfWorkManager);
    }

    /// <summary>
    /// 工作单元拦截器。<see cref="DispatchProxy"/> 要求无参构造，目标对象通过 <see cref="SetTarget"/> 注入。
    /// </summary>
    /// <remarks>必须是 public 且非 sealed：DispatchProxy 会在运行期生成它的子类。</remarks>
    public class UnitOfWorkInterceptor : DispatchProxy
    {
        private object _target = null!;
        private IUnitOfWorkManager _manager = null!;

        internal void SetTarget(object target, IUnitOfWorkManager manager)
        {
            _target = target;
            _manager = manager;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null) throw new ArgumentNullException(nameof(targetMethod));

            var attribute = targetMethod.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true)
                            ?? FindTargetMethod(targetMethod)?.GetCustomAttribute<UnitOfWorkAttribute>(inherit: true)
                            ?? _target.GetType().GetCustomAttribute<UnitOfWorkAttribute>(inherit: true);

            // 未标注 [UnitOfWork]：直接调用目标，不引入 UOW
            if (attribute is null)
                return InvokeTarget(targetMethod, args);

            var options = new UnitOfWorkOptions
            {
                IsAutoRollback = attribute.IsAutoRollback,
                Timeout = attribute.Timeout
            };

            var unitOfWork = _manager.Begin(options);
            var cancellationToken = FindCancellationToken(args);

            object? result;
            try
            {
                result = InvokeTarget(targetMethod, args);
            }
            catch
            {
                // 同步异常：立即回滚，保留原始异常
                _ = unitOfWork.RollbackAsync(CancellationToken.None);
                throw;
            }

            return RewriteResult(result, targetMethod.ReturnType, unitOfWork, cancellationToken);
        }

        /// <summary>
        /// 找到接口方法对应的实现类方法，用于读取实现方法上的 <see cref="UnitOfWorkAttribute"/>。
        /// </summary>
        private System.Reflection.MethodInfo? FindTargetMethod(System.Reflection.MethodInfo interfaceMethod)
        {
            var parameterCount = interfaceMethod.GetParameters().Length;
            return _target.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == interfaceMethod.Name
                                     && m.GetParameters().Length == parameterCount);
        }

        private object? InvokeTarget(MethodInfo method, object?[]? args)
        {
            try
            {
                return method.Invoke(_target, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                // 还原原始异常的堆栈
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static CancellationToken FindCancellationToken(object?[]? args)
        {
            if (args is null) return CancellationToken.None;

            foreach (var arg in args)
            {
                if (arg is CancellationToken token)
                    return token;
            }

            return CancellationToken.None;
        }

        private static object? RewriteResult(object? result, Type returnType, IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            if (returnType == typeof(Task))
                return WrapTask((Task)result!, unitOfWork, cancellationToken);

            if (returnType == typeof(ValueTask))
                return WrapValueTask((ValueTask)result!, unitOfWork, cancellationToken);

            if (returnType.IsGenericType)
            {
                var definition = returnType.GetGenericTypeDefinition();

                if (definition == typeof(Task<>))
                {
                    return WrapGenericTask(result!, returnType.GetGenericArguments()[0], unitOfWork, cancellationToken);
                }

                if (definition == typeof(ValueTask<>))
                {
                    var asTask = returnType.GetMethod("AsTask")!;
                    var task = (Task)asTask.Invoke(result, null)!;
                    return WrapGenericTask(task, returnType.GetGenericArguments()[0], unitOfWork, cancellationToken);
                }
            }

            // 同步返回值：立即提交
            unitOfWork.CompleteAsync(cancellationToken).GetAwaiter().GetResult();
            return result;
        }

        private static async Task WrapTask(Task task, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        {
            try
            {
                await task.ConfigureAwait(false);
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RollbackQuietlyAsync(unitOfWork).ConfigureAwait(false);
                throw;
            }
        }

        private static async ValueTask WrapValueTask(ValueTask valueTask, IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            try
            {
                await valueTask.ConfigureAwait(false);
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RollbackQuietlyAsync(unitOfWork).ConfigureAwait(false);
                throw;
            }
        }

        private static object WrapGenericTask(object task, Type resultType, IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            var method = typeof(UnitOfWorkInterceptor)
                .GetMethod(nameof(WrapGenericTaskCore), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return method.Invoke(null, new object?[] { task, unitOfWork, cancellationToken })!;
        }

        private static async Task<T> WrapGenericTaskCore<T>(object task, IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            try
            {
                var value = await ((Task<T>)task).ConfigureAwait(false);
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return value;
            }
            catch
            {
                await RollbackQuietlyAsync(unitOfWork).ConfigureAwait(false);
                throw;
            }
        }

        private static async Task RollbackQuietlyAsync(IUnitOfWork unitOfWork)
        {
            try
            {
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // 回滚失败不应覆盖原始异常
            }
        }
    }
}
