using System;
using Domain.Projects;
using Domain.Tasks;
using Frame.Core;
using Frame.Core.Application;
using Frame.Core.Modules;
using Frame.Databases;
using Frame.EventBus;
using Frame.Locks;
using Frame.Locks.Enums;
using Infrastructure.DatabaseContexts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    /// <summary>TaskBoard 的组合根：Web 与 Worker 共用同一套注册。</summary>
    public static class TaskBoardServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 TaskBoard 的数据库、仓储、应用服务、事件总线与本地锁。
        /// <paramref name="configure"/> 用于追加宿主专属能力（例如 Worker 注册调度器）。
        /// </summary>
        public static IServiceCollection AddTaskBoard(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<ServiceConfigurationContext>? configure = null)
        {
            var connection = configuration["TaskBoard:Connections:Database"];
            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new InvalidOperationException(
                    "配置节 TaskBoard:Connections:Database 未设置。请参考 appsettings.json 填写连接字符串。");
            }

            services.AddFrameService(options =>
            {
                options.UseModule();
                options.UseApplication();
                options.UseDatabaseContext<TaskBoardDatabaseContext>(new ConnectionStringCollection(connection));
                options.UseEventBus();
                options.UseLock(LockType.Local);

                configure?.Invoke(options);
            });

            return services;
        }
    }
}
