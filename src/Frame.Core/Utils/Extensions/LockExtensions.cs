using System;

namespace Frame.Core
{
    /// <summary>
    /// 提供简化对象加锁操作的扩展方法
    /// </summary>
    public static class LockExtensions
    {
        /// <summary>
        /// 通过对给定的 <paramref name="source"/> 对象加锁来执行指定的 <paramref name="action"/>
        /// </summary>
        /// <param name="source">要加锁的源对象</param>
        /// <param name="action">要执行的操作</param>
        public static void Locking(this object source, Action action)
        {
            lock (source)
            {
                action();
            }
        }

        /// <summary>
        /// 通过对给定的泛型 <paramref name="source"/> 对象加锁来执行指定的 <paramref name="action"/>
        /// </summary>
        /// <typeparam name="T">要加锁对象的类型</typeparam>
        /// <param name="source">要加锁的源对象</param>
        /// <param name="action">接收对象参数的操作</param>
        public static void Locking<T>(this T source, Action<T> action) where T : class
        {
            lock (source)
            {
                action(source);
            }
        }

        /// <summary>
        /// 通过对给定的 <paramref name="source"/> 对象加锁来执行指定的 <paramref name="func"/> 并返回结果
        /// </summary>
        /// <typeparam name="TResult">返回结果的类型</typeparam>
        /// <param name="source">要加锁的源对象</param>
        /// <param name="func">要执行的函数</param>
        /// <returns>函数执行后的返回值</returns>
        public static TResult Locking<TResult>(this object source, Func<TResult> func)
        {
            lock (source)
            {
                return func();
            }
        }

        /// <summary>
        /// 通过对给定的泛型 <paramref name="source"/> 对象加锁来执行指定的 <paramref name="func"/> 并返回结果
        /// </summary>
        /// <typeparam name="T">要加锁对象的类型</typeparam>
        /// <typeparam name="TResult">返回结果的类型</typeparam>
        /// <param name="source">要加锁的源对象</param>
        /// <param name="func">接收对象参数的函数</param>
        /// <returns>函数执行后的返回值</returns>
        public static TResult Locking<T, TResult>(this T source, Func<T, TResult> func) where T : class
        {
            lock (source)
            {
                return func(source);
            }
        }
    }
}
