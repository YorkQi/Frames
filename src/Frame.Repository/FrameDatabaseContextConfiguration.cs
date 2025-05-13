using Frame.Core;
using Frame.Repository.Databases;
using Frame.Repository.DBContexts;
using Microsoft.Extensions.DependencyInjection;

namespace Frame.Repository
{
    public static class FrameDatabaseContextConfiguration
    {
        public static void UseDatabaseContext<TDatabaseContext>(this FrameConfiguration configuration, DBConnectionString dbconnectionStr) where TDatabaseContext : DatabaseContext, new()
        {
            configuration.Add(ServiceDescriptor.Scoped((provider) =>
            {
                var databaseContext = new TDatabaseContext();
                databaseContext.Initialize(provider, dbconnectionStr);
                return databaseContext;
            }));

            configuration.AddRepository();

        }


        private static void AddRepository(this FrameConfiguration configuration)
        {
            var assemblyType = configuration.GetAssemblyType();
            foreach (var type in assemblyType.Types)
            {
                if (type.IsClass)
                {
                    var interfaceTypes = type.GetInterfaces();//取得仓储类继承的所有接口
                    foreach (var interfaceType in interfaceTypes)
                    {
                        var imps = interfaceType.GetInterfaces();//取得仓储类继承的接口网上继承的接口
                        foreach (var imp in imps)
                        {
                            //如果往上继承的接口泛型接口并且继承IRepository接口则就是想要的接口
                            if (imp.IsGenericType && imp.GetInterface(nameof(IRepository)) != null)
                            {
                                configuration.Add(ServiceDescriptor.Scoped(interfaceType, type));
                                configuration.Add(ServiceDescriptor.Scoped(imp, type));
                            }
                        }
                    }
                }
            }
        }
    }
}
