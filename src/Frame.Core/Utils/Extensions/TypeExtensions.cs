using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Core
{
    public static class TypeExtensions
    {

        /// <summary>
        /// 获取包含程序集名称的完整类型名称
        /// </summary>
        public static string GetFullNameWithAssemblyName(this Type type)
        {
            return type.FullName + ", " + type.Assembly.GetName().Name;
        }

        /// <summary>
        /// 判断当前类型实例是否可以赋值给目标类型TTarget的实例
        /// 内部使用Type.IsAssignableFrom方法(反向)
        /// </summary>
        /// <typeparam name="TTarget">目标类型</typeparam>
        public static bool IsAssignableTo<TTarget>([NotNull] this Type type)
        {
            Check.NotNull(type, nameof(type));
            return type.IsAssignableTo(typeof(TTarget));
        }

        /// <summary>
        /// 判断当前类型实例是否可以赋值给目标类型的实例
        /// 内部使用Type.IsAssignableFrom方法(反向)
        /// </summary>
        /// <param name="type">当前类型</param>
        /// <param name="targetType">目标类型</param>
        public static bool IsAssignableTo([NotNull] this Type type, [NotNull] Type targetType)
        {
            Check.NotNull(type, nameof(type));
            Check.NotNull(targetType, nameof(targetType));
            return targetType.IsAssignableFrom(type);
        }

        /// <summary>
        /// 获取该类型的所有基类
        /// </summary>
        /// <param name="type">要获取基类的类型</param>
        /// <param name="includeObject">为true时，在返回数组中包含标准object类型</param>
        public static Type[] GetBaseClasses([NotNull] this Type type, bool includeObject = true)
        {
            Check.NotNull(type, nameof(type));
            var types = new List<Type>();
            AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject);
            return types.ToArray();
        }


        /// <summary>
        /// 获取该类型的所有基类
        /// </summary>
        /// <param name="type">要获取基类的类型</param>
        /// <param name="stoppingType">用于停止继续查找更深层次基类的类型。该类型将被包含在返回数组中</param>
        /// <param name="includeObject">为true时，在返回数组中包含标准object类型</param>
        public static Type[] GetBaseClasses([NotNull] this Type type, Type stoppingType, bool includeObject = true)
        {
            Check.NotNull(type, nameof(type));

            var types = new List<Type>();
            AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
            return types.ToArray();
        }

        /// <summary>
        /// 递归添加类型及其基类
        /// </summary>
        /// <param name="types">类型列表</param>
        /// <param name="type">当前类型</param>
        /// <param name="includeObject">是否包含object类型</param>
        /// <param name="stoppingType">停止类型</param>
        private static void AddTypeAndBaseTypesRecursively(
            [NotNull] List<Type> types,
            Type? type,
            bool includeObject,
            Type? stoppingType = null)
        {
            if (type == null || type == stoppingType)
            {
                return;
            }

            if (!includeObject && type == typeof(object))
            {
                return;
            }

            AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
            types.Add(type);
        }

    }
}
