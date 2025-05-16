using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Frame.Core
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// 检查给定的集合对象是否为 null 或没有元素。
        /// </summary>
        public static bool IsNullOrEmpty<T>(this ICollection<T>? source)
        {
            return source == null || source.Count <= 0;
        }

        /// <summary>
        /// 如果元素不在集合中，则将其添加到集合。
        /// </summary>
        /// <param name="source">目标集合</param>
        /// <param name="item">要检查并添加的元素</param>
        /// <typeparam name="T">集合元素的类型</typeparam>
        /// <returns>如果成功添加返回 true，否则返回 false</returns>
        public static bool AddIfNotContains<T>([NotNull] this ICollection<T> source, T item)
        {
            Check.NotNull(source, nameof(source));

            if (source.Contains(item))
            {
                return false;
            }

            source.Add(item);
            return true;
        }

        /// <summary>
        /// 添加集合中不存在的多个元素到目标集合。
        /// </summary>
        /// <param name="source">目标集合</param>
        /// <param name="items">要检查并添加的元素集合</param>
        /// <typeparam name="T">集合元素的类型</typeparam>
        /// <returns>返回实际被添加的元素</returns>
        public static IEnumerable<T> AddIfNotContains<T>([NotNull] this ICollection<T> source, IEnumerable<T> items)
        {
            Check.NotNull(source, nameof(source));

            var addedItems = new List<T>();

            foreach (var item in items)
            {
                if (source.Contains(item))
                {
                    continue;
                }

                source.Add(item);
                addedItems.Add(item);
            }

            return addedItems;
        }

        /// <summary>
        /// 根据给定的 <paramref name="predicate"/> 谓词条件，如果集合中不存在满足条件的元素，则通过工厂方法添加新元素。
        /// </summary>
        /// <param name="source">目标集合</param>
        /// <param name="predicate">判断元素是否存在的条件</param>
        /// <param name="itemFactory">生成新元素的工厂方法</param>
        /// <typeparam name="T">集合元素的类型</typeparam>
        /// <returns>如果成功添加返回 true，否则返回 false</returns>
        public static bool AddIfNotContains<T>([NotNull] this ICollection<T> source, [NotNull] Func<T, bool> predicate, [NotNull] Func<T> itemFactory)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(predicate, nameof(predicate));
            Check.NotNull(itemFactory, nameof(itemFactory));

            if (source.Any(predicate))
            {
                return false;
            }

            source.Add(itemFactory());
            return true;
        }

        /// <summary>
        /// 移除集合中所有满足给定 <paramref name="predicate"/> 条件的元素。
        /// </summary>
        /// <typeparam name="T">集合元素的类型</typeparam>
        /// <param name="source">目标集合</param>
        /// <param name="predicate">元素移除条件</param>
        /// <returns>被移除的元素列表</returns>
        public static IList<T> RemoveAll<T>([NotNull] this ICollection<T> source, Func<T, bool> predicate)
        {
            var items = source.Where(predicate).ToList();

            foreach (var item in items)
            {
                source.Remove(item);
            }

            return items;
        }

        /// <summary>
        /// 从集合中移除指定的多个元素。
        /// </summary>
        /// <typeparam name="T">集合元素的类型</typeparam>
        /// <param name="source">目标集合</param>
        /// <param name="items">要移除的元素集合</param>
        public static void RemoveAll<T>([NotNull] this ICollection<T> source, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                source.Remove(item);
            }
        }
        /// <summary>
        /// 返回集合中随机的一个元素
        /// </summary>
        /// <typeparam name="T">集合元素的类型</typeparam>
        /// <param name="source">目标集合</param>
        /// <param name="rng">随机对象</param>
        /// <returns>返回随机的元素</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static T RandomElement<T>([NotNull] this ICollection<T> source, Random? rng = null)
        {
            Check.NotNull(source, nameof(source));

            var random = rng ?? new Random();
            using var enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Sequence is empty");

            T current = enumerator.Current;
            int count = 1;

            while (enumerator.MoveNext())
            {
                count++;
                if (random.Next(count) == 0)
                {
                    current = enumerator.Current;
                }
            }

            return current ?? throw new InvalidOperationException("Sequence contains null elements");
        }
    }

}
