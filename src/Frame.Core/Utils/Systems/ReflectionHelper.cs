using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Frame.Core
{
    internal static class ReflectionHelper
    {
        public static IReadOnlyList<Type> GetAssemblyAllExportedTypes()
        {
            return GetAssemblyExportedTypes(GetAssembliesAll());
        }

        public static IReadOnlyList<Type> GetAssemblyAllTypes()
        {
            return GetAssemblyTypes(GetAssembliesAll());
        }

        public static IReadOnlyList<Type> GetAssemblyExportedTypes(IEnumerable<Assembly> assemblies)
        {
            List<Type> assembliesAllType = [];
            foreach (var assembly in assemblies)
            {
                var assemblyTypes = GetAssemblyExportedTypes(assembly);
                if (assemblyTypes.Any())
                {
                    assembliesAllType.AddRange(assemblyTypes);
                }
            }
            return assembliesAllType;
        }
        public static IReadOnlyList<Type> GetAssemblyTypes(IEnumerable<Assembly> assemblies)
        {
            List<Type> assembliesAllType = [];
            foreach (var assembly in assemblies)
            {
                var assemblyTypes = GetAssemblyType(assembly);
                if (assemblyTypes.Any())
                {
                    assembliesAllType.AddRange(assemblyTypes);
                }
            }
            return assembliesAllType;
        }

        public static IReadOnlyList<Type> GetAssemblyExportedTypes(Assembly assembly)
        {
            return assembly.GetExportedTypes();
        }
        public static IReadOnlyList<Type> GetAssemblyType(Assembly assembly)
        {
            return assembly.GetTypes();
        }

        public static IEnumerable<Assembly> GetAssembliesAll()
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
