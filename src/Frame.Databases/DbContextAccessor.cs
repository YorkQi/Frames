using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Frame.Databases
{
    /// <summary>
    /// [已过时] IDbContext 的 Ambient Context 访问器。
    /// 通过 AsyncLocal 存储当前异步上下文中的 <see cref="IDbContext"/> 实例。
    /// </summary>
    [Obsolete("请改用构造函数注入 IDbContext。\r\n" +
              "旧方式：DbContextAccessor.Current.Get<T>(id)\r\n" +
              "新方式：注入 IDbContext dbContext → dbContext.Get<T>(id)")]
    public static class DbContextAccessor
    {
        private static readonly AsyncLocal<IDbContext?> _current = new();

        /// <summary>获取当前作用域中的 IDbContext 实例。若未初始化则抛出异常。</summary>
        public static IDbContext Current => _current.Value
            ?? throw new InvalidOperationException(
                "IDbContext 未在当前上下文中初始化。请确保已通过 DatabaseContext 创建作用域。");

        /// <summary>设置当前作用域的 IDbContext 实例。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Set(IDbContext context)
        {
            _current.Value = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>清除当前作用域的 IDbContext 实例。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Clear()
        {
            _current.Value = null;
        }
    }
}
