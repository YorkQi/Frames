using System;
using System.Diagnostics.CodeAnalysis;
using Frame.Core;
using Frame.Redis.Enums;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Frame.Redis
{
    /// <summary>
    /// Redis 数据库配置扩展方法。
    /// </summary>
    public static class RedisContextConfiguration
    {
        /// <summary>
        /// 注册 Redis 数据库服务。
        /// 服务注册清单：
        /// <list type="bullet">
        ///   <item><see cref="ConnectionMultiplexer"/> (Singleton) — Redis TCP 连接池复用器</item>
        ///   <item><typeparamref name="TRedisContext"/> (Scoped) — Redis 数据库上下文</item>
        ///   <item><see cref="RedisConnectionStringCollection"/> (Singleton) — 连接字符串集合</item>
        /// </list>
        /// </summary>
        /// <typeparam name="TRedisContext">自定义 RedisContext 类型</typeparam>
        /// <param name="configuration">框架配置上下文</param>
        /// <param name="redisConnections">Redis 连接字符串集合</param>
        /// <param name="strategy">连接选择策略（默认 Random）</param>
        public static ServiceConfigurationContext UseRedisDatabase<TRedisContext>(
            [NotNull] this ServiceConfigurationContext configuration,
            [NotNull] RedisConnectionStringCollection redisConnections,
            ConnectionStringStrategy strategy = ConnectionStringStrategy.Random)
            where TRedisContext : RedisContext, new()
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));
            if (redisConnections is null) throw new ArgumentNullException(nameof(redisConnections));

            // 注册连接字符串集合（Singleton）
            configuration.Add(ServiceDescriptor.Singleton(redisConnections));

            // 【优化】ConnectionMultiplexer 注册为 Singleton，全局复用 TCP 连接池
            var connectionString = redisConnections.GetConnection(strategy);
            var multiplexer = ConnectionMultiplexer.Connect(connectionString);
            configuration.Add(ServiceDescriptor.Singleton(multiplexer));

            // 注册 RedisContext（Scoped，每个请求一个实例）
            configuration.Add(ServiceDescriptor.Scoped<TRedisContext>(sp =>
            {
                var mux = sp.GetRequiredService<ConnectionMultiplexer>();
                return new TRedisContext
                {
                    Multiplexer = mux
                };
            }));

            return configuration;
        }
    }
}
