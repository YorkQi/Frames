using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Frame.Databases
{
    /// <summary>
    /// IDbContext 的 Ambient Context 访问器。
    /// 通过 <see cref="AsyncLocal{T}"/> 存储当前异步执行上下文中的 <see cref="IDbContext"/> 实例，
    /// 使 <see cref="Repositories.Repository{TPrimaryKey, TEntity}"/> 无需通过构造函数注入即可获取 DBContext。
    /// </summary>
    public static class DbContextAccessor
    {
        private static readonly AsyncLocal<IDbContext?> _current = new();

        /// <summary>
        /// 获取当前作用域中的 <see cref="IDbContext"/> 实例。
        /// 若未初始化则抛出 <see cref="InvalidOperationException"/>。
        /// </summary>
        public static IDbContext Current => _current.Value
            ?? throw new InvalidOperationException(
                "IDbContext 未在当前上下文中初始化。请确保已通过 DatabaseContext 创建作用域。");

        /// <summary>
        /// 设置当前作用域的 <see cref="IDbContext"/> 实例。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Set(IDbContext context)
        {
            _current.Value = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 清除当前作用域的 <see cref="IDbContext"/> 实例。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Clear()
        {
            _current.Value = null;
        }
    }
}
