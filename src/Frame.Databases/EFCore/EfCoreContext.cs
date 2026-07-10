using System;
using System.Collections.Generic;
using System.Reflection;
using Frame.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frame.Databases.EFCore
{
    /// <summary>
    /// EF Core 最小 DbContext 基类。
    /// OnModelCreating 自动扫描所有程序集中实现了 IEntity 的实体类型（结果缓存）。
    /// 用户也可继承此类添加自定义配置。
    /// </summary>
    public class EfCoreContext(DbContextOptions options) : DbContext(options)
    {
        /// <summary>缓存扫描到的 IEntity 实现类型，避免每次创建 DbContext 时全量扫描程序集</summary>
        private static readonly Lazy<IReadOnlyList<Type>> _entityTypes = new(() =>
        {
            var result = new List<Type>();
            var entityType = typeof(IEntity);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                if (name is null ||
                    name.StartsWith("System", StringComparison.Ordinal) ||
                    name.StartsWith("Microsoft", StringComparison.Ordinal) ||
                    name.StartsWith("netstandard", StringComparison.Ordinal))
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException) { continue; }

                foreach (var type in types)
                {
                    if (type.IsClass && !type.IsAbstract && entityType.IsAssignableFrom(type))
                    {
                        result.Add(type);
                    }
                }
            }

            return result;
        });

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var type in _entityTypes.Value)
            {
                modelBuilder.Entity(type);
            }
        }
    }
}
