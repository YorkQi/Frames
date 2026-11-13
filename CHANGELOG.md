# 更新日志

本项目尚未发布 NuGet 包，版本号从 `0.1.0` 起按「框架能力」递增。格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [Unreleased]

### 文档

- 重写根 `README.md`：模块表、依赖图、快速开始、文档导航、能力状态表。
- 新增 `docs/` 文档集：索引、快速开始、架构、核心、数据库、锁、事件总线、调度、Redis、示例、设计说明、测试。
- 新增 `AGENTS.md`：面向 AI 编码助手与新人的环境、命令、编码约定、实现现状与改动检查清单。
- 新增 `CONTRIBUTING.md` 与 `CHANGELOG.md`。
- 全站文档统一使用中文（保留代码、类型名、异常消息的原文）。
- 修正原 README 中与代码不符的描述：
  - 删除不存在的审计接口（`IHasCreationTime`、`IModificationAuditedObject`、`ISoftDelete`、`IDeletionAuditedObject`）；
  - 说明 `[UnitOfWork]` 特性当前没有拦截器，不会自动开启事务；
  - 说明领域事件只被收集、没有分发器；
  - 说明模块的 `OnApplicationInitializationAsync` / `OnApplicationShutdownAsync` 不会被调用；
  - 说明 EF Core 通道的写操作是延迟持久化，必须显式 `SaveChanges`；
  - 修正「仓储由 `UseApplication()` 注册」为「由 `UseDatabaseContext()` 内部的 `UseRepositories()` 注册」；
  - 补充 `Frame.Redis` 的 `ConnectionMultiplexer` 生命周期与 `RedisDbContext.Dispose` 关闭全局连接的问题。

## [0.1.0] - 未发布

### 新增

- `Frame.Core`：实体基类、仓储契约、工作单元、应用服务基类、`[ServiceModule]`/`[DependsOn]` 模块系统、`AddFrameService` 程序集发现与装配。
- `Frame.Databases`：Dapper + EF Core 双通道 `IDbContext`、`DatabaseContext` 作用域、三种方言（MySQL / SQL Server / PostgreSQL）的分页 SQL、`DbParameters`、`PageInput`/`PageResult`。
- `Frame.Locks`：`ILock`/`ILockFactory`、进程内 `Monitor` 锁、基于 RedLock.net 的 Redis 分布式锁。
- `Frame.EventBus`：进程内事件总线、`IHostedService` 消费循环、指数退避重试。
- `Frame.Scheduler`：Quartz 包装、`[Scheduled]` 特性扫描与配置列表两种注册方式、5 段 cron 自动补秒。
- `Frame.Redis`：`RedisContext` + `IRedisDbContext`，覆盖 Key/String/List/Set/Hash/SortedSet/PubSub 常用命令。
- `samples/Webs`：ASP.NET Core 五层 DDD 示例；`samples/Scheduler/Job`：定时任务示例。
- `Test/TestUnit`：实体、分页、连接串、参数、特性等纯逻辑单元测试。
