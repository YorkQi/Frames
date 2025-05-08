using Frame.Core;
using Frame.Repository.Databases;
using Frame.Repository.DBContexts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Frame.Repository
{
    public static class FrameDatabaseContextConfiguration
    {
        public static void UseDatabaseContext<TDatabaseContext>(this FrameConfiguration configuration, DBConnectionString dbconnectionStr) where TDatabaseContext : DatabaseContext, new()
        {
            configuration.Add(new ServiceDescriptor(typeof(TDatabaseContext), (provider) =>
            {
                var databaseContext = new TDatabaseContext();
                databaseContext.Initialize(provider, dbconnectionStr);
                return databaseContext;
            }, ServiceLifetime.Scoped));

            var assemblies = GetAssembliesAll();
            foreach (var assembly in assemblies)
            {
                var exportedTypes = assembly.GetExportedTypes();
                foreach (var classType in exportedTypes)
                {
                    if (classType.IsClass)
                    {
                        var interfaceTypes = classType.GetInterfaces();//取得仓储类继承的所有接口
                        foreach (var interfaceType in interfaceTypes)
                        {
                            var imps = interfaceType.GetInterfaces();//取得仓储类继承的接口网上继承的接口
                            foreach (var imp in imps)
                            {
                                //如果往上继承的接口泛型接口并且继承IRepository接口则就是想要的接口
                                if (imp.IsGenericType && imp.GetInterface(nameof(IRepository)) != null)
                                {
                                    configuration.Add(new ServiceDescriptor(interfaceType, classType, ServiceLifetime.Scoped));
                                    configuration.Add(new ServiceDescriptor(imp, classType, ServiceLifetime.Scoped));
                                }
                            }
                        }
                    }
                }
            }
        }


        private static IEnumerable<Assembly> GetAssembliesAll()
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
