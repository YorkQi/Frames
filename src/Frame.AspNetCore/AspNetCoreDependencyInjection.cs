using Frame.AspNetCore.Filters;
using Frame.Core.Application;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AspNetCoreDependencyInjection
    {
        public static IServiceCollection AddAspNetCore(this IServiceCollection services)
        {
            services.AddMvcCore(op =>
            {
                //op.Filters.Add(typeof(ModelValidattionAttribute));//验证DTO
                op.Filters.Add(typeof(ExceptionFilter));//异常
            });
            return services;
        }

        public static IServiceCollection AddModule(this IServiceCollection services)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));
            var types = assembly.GetExportedTypes();
            services.Injection(types);
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach (var item in referencedAssemblies)
            {
                Assembly itemAssembly = Assembly.Load(item);
                var itemTypes = itemAssembly.GetExportedTypes();
                services.Injection(itemTypes);
            }
            return services;
        }

        public static IServiceCollection AddModule<TModule>(this IServiceCollection services) where TModule : class
        {
            Assembly? assembly = Assembly.GetAssembly(typeof(TModule));
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));
            var types = assembly.GetExportedTypes();
            services.Injection(types);
            return services;
        }


        public static IServiceCollection AddApplication<TModule>(this IServiceCollection services) where TModule : class
        {
            Assembly? assembly = Assembly.GetAssembly(typeof(TModule));
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));
            Type[] types = assembly.GetExportedTypes();
            foreach (Type type in types)
            {
                if (type.IsPublic || type.IsClass || type.IsAbstract)
                {
                    var imps = type.GetInterfaces();
                    if (imps.Any(t => t.Equals(typeof(IApplication))))
                    {
                        services.Injection(type);
                    }
                }
            }
            return services;
        }
    }
}
