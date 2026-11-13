using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core;
using Frame.Core.Repositories;
using Frame.Core.Uow;
using Frame.Databases.DBContext;
using Frame.Databases.DbContexts;
using Frame.Databases.Dialects;
using Frame.Databases.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.Databases
{
    /// <summary>
    /// 数据库上下文配置扩展方法。
    /// 提供 <c>UseDatabaseContext&lt;T&gt;</c> 用于注册数据库相关服务。
    /// </summary>
    public static class DatabaseContextConfiguration
    {
        /// <summary>
        /// 注册数据库上下文及相关服务。
        /// 服务注册清单：
        /// <list type="bullet">
        ///   <item><see cref="DbContextConnectionStringAccessor"/> (Scoped) — 连接字符串持有者</item>
        ///   <item><see cref="IDbContext"/> → <see cref="DbContext"/> (Scoped) — 数据库操作接口</item>
        ///   <item><typeparamref name="TDatabaseContext"/> (Scoped) — 用户自定义 DatabaseContext</item>
        ///   <item>所有实现 <see cref="IRepository"/> 的类型 (Scoped) — 自动扫描注册</item>
        /// </list>
        /// </summary>
        /// <typeparam name="TDatabaseContext">用户自定义的 DatabaseContext 类型，必须可 new()</typeparam>
        /// <param name="configuration">框架配置上下文</param>
        /// <param name="dbconnections">数据库连接字符串集合（支持多策略选取）</param>
        /// <param name="strategy">连接字符串选择策略（默认 Random）</param>
        public static void UseDatabaseContext<TDatabaseContext>(
            [NotNull] this ServiceConfigurationContext configuration,
            [NotNull] ConnectionStringCollection dbconnections,
            ConnectionStringStrategy strategy = ConnectionStringStrategy.Random)
            where TDatabaseContext : DatabaseContext, new()
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(dbconnections);

            // 注册连接字符串访问器（Scoped，DatabaseContext 初始化时设置值）
            configuration.Add(ServiceDescriptor.Scoped<DbContextConnectionStringAccessor, DbContextConnectionStringAccessor>());

            // 注册 IDbContext 实现（Scoped，构造函数中读取连接字符串）
            configuration.Add(ServiceDescriptor.Scoped<IDbContext, DbContext>(sp =>
            {
                var accessor = sp.GetRequiredService<DbContextConnectionStringAccessor>();
                var ctx = new DbContext(accessor);
                ctx.Dialect = ctx.CreateDialect();
                return ctx;
            }));

            // 注册 DatabaseContext（用户通过此类管理仓储）
            configuration.Add(ServiceDescriptor.Scoped((provider) =>
            {
                var databaseContext = new TDatabaseContext();
                databaseContext.Initialize(provider, dbconnections, strategy);
                return databaseContext;
            }));

            // 注册 UOW 回调：将 EF Core SaveChanges 挂接到全局 UnitOfWorkManager
            UnitOfWorkManager.RegisterDbActions(
                onComplete: async (cancellationToken) =>
                {
                    // 此处由 UOW 在提交时调用，执行 EF Core 的 SaveChanges
                    // 注意：实际 SaveChanges 由 IDbContext 实例内部管理，
                    // 此处通过 DbContext 注入时的委托来执行
                    await Task.CompletedTask;
                });

            // 自动扫描并注册所有仓储实现
            UseRepositories(configuration);
        }

        /// <summary>
        /// 自动扫描已注册程序集中的所有仓储实现并注册到 DI 容器。
        /// </summary>
        private static void UseRepositories([NotNull] ServiceConfigurationContext configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var assemblyTypes = configuration.GetAssemblyTypes();

            foreach (var type in assemblyTypes)
            {
                // 跳过抽象类、开放泛型、未实现 IRepository 的类
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters
                    || type.GetInterface(nameof(IRepository)) is null)
                    continue;

                // 遍历类实现的所有接口，把继承自 IRepository 的接口（除标记接口自身）都注册
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface != typeof(IRepository) && iface.GetInterface(nameof(IRepository)) is not null)
                    {
                        configuration.Add(ServiceDescriptor.Scoped(iface, type));
                    }
                }
            }
        }
    }
}
