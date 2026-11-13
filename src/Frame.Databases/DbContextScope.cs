using System.Threading;

namespace Frame.Databases
{
    /// <summary>
    /// 当前作用域中的 IDbContext 持有者。
    /// </summary>
    internal static class DbContextScope
    {
        private static readonly AsyncLocal<IDbContext?> _current = new();

        /// <summary>获取当前作用域的 IDbContext，可能为 null。</summary>
        public static IDbContext? Current => _current.Value;

        /// <summary>设置当前作用域的 IDbContext（由 DatabaseContext 调用）。</summary>
        public static void Set(IDbContext context) => _current.Value = context;

        /// <summary>清除当前作用域的 IDbContext（由 DatabaseContext 调用）。</summary>
        public static void Clear() => _current.Value = null;
    }
}
