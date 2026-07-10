using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Frame.Core.Utils
{
    internal static class ReflectionHelper
    {
        /// <summary>
        /// 获取当前 AppDomain 中所有程序集导出的公开类型
        /// </summary>
        public static IReadOnlyList<Type> GetAllAssemblyExportedTypes()
        {
            var types = new List<Type>();
            foreach (var assembly in GetAllAssemblies())
            {
                types.AddRange(assembly.GetExportedTypes());
            }
            return types;
        }

        private static IEnumerable<Assembly> GetAllAssemblies()
        {
            var assemblies = new List<Assembly>();
            var basePath = AppContext.BaseDirectory;
            foreach (var dll in Directory.GetFiles(basePath, "*.dll"))
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(dll));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载失败：{dll}，错误：{ex.Message}");
                }
            }
            return assemblies;
        }
    }
}
