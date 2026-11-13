# 文档索引

本目录是 Frames 的设计与使用文档。根目录 [README.md](../README.md) 只保留定位、模块表和最小上手示例，细节全部在这里。

## 阅读路径

**第一次接触，想跑起来**
[快速开始](getting-started.md) → [示例工程](samples.md) → [整体架构](architecture.md)

**要在业务项目里使用**
[整体架构](architecture.md) → [核心模块](core.md) → [工作单元与自动事务](unit-of-work.md) → [数据库访问](databases.md) → 按需：[审计字段](auditing.md) / [领域事件](domain-events.md) / [锁](locks.md) / [事件总线](eventbus.md) / [定时任务](scheduler.md) / [Redis](redis.md)

**要改框架代码**
[整体架构](architecture.md) → [模块与目录结构](modules.md) → [设计说明与已知差距](design-notes.md) → 对应模块文档 → [测试说明](testing.md) → [贡献指南](../CONTRIBUTING.md)

**只想确认某能力是否可用**
[设计说明与已知差距](design-notes.md) 的「设计目标 vs 现状」表

## 文档清单

| 文件 | 回答的问题 |
| --- | --- |
| [getting-started.md](getting-started.md) | 需要什么环境？最小的可运行工程长什么样？启动报错怎么排查？ |
| [architecture.md](architecture.md) | 代码怎么分层？程序集怎么被发现？每个服务是什么生命周期？一次请求经过哪些环节？ |
| [core.md](core.md) | `Entity` 的相等性规则？仓储契约有哪些方法？UOW 怎么嵌套与回滚？模块系统如何工作？ |
| [unit-of-work.md](unit-of-work.md) | `[UnitOfWork]` 怎么生效？提交/回滚的边界在哪？为什么嵌套不重复提交？ |
| [domain-events.md](domain-events.md) | 事件什么时候被收集、什么时候分发？会不会丢？处理器要幂等吗？ |
| [auditing.md](auditing.md) | 审计字段谁填充？怎么替换时间/用户来源？软删除有什么后果？ |
| [modules.md](modules.md) | 每个程序集有哪些目录？模块怎么被发现/排序？生命周期钩子何时执行？ |
| [databases.md](databases.md) | EF Core 与 Dapper 如何共存？为什么插入返回 0？分页 SQL 怎么生成？多连接串怎么选？ |
| [locks.md](locks.md) | 本地锁和 RedLock 的区别？拿不到锁会怎样？锁的过期与清理？ |
| [eventbus.md](eventbus.md) | 事件如何投递？为什么是串行的？失败重试几次？会丢事件吗？ |
| [scheduler.md](scheduler.md) | 两种注册方式？Cron 支持几段？作业实例从哪来？重启后任务还在吗？ |
| [redis.md](redis.md) | 支持哪些数据结构？多路复用器何时创建/关闭？有哪些坑？ |
| [samples.md](samples.md) | 三个示例怎么跑？TaskBoard 项目集怎么分层、怎么共用组合根？表结构在哪？ |
| [design-notes.md](design-notes.md) | 为什么这样设计？哪些设计与实现不一致？技术债的优先级？ |
| [testing.md](testing.md) | 测试覆盖了什么？没覆盖什么？怎么补？ |

## 文档约定

1. **以代码为准**：文档描述的是当前仓库代码的行为。若文档与代码冲突，以代码为准，并在 [design-notes.md](design-notes.md) 记录差异、提 issue 修正文档。
2. **区分「已实现」与「设计意图」**：已实现但存在边界的，文中标注 `⚠️ 限制`；仅保留骨架、尚未接线的，标注 `⚠️ 未接线` / `❌ 不存在`。不把设计意图写成可用特性。
3. **API 签名照抄代码**：类型名、方法名、参数顺序、异常消息保持与源码一致，便于检索。
4. **改动同步**：新增/修改公共 API 时，同步更新对应模块文档与 [CHANGELOG.md](../CHANGELOG.md)；影响用法时更新 [getting-started.md](getting-started.md) 与根 README 的示例。
5. **中文优先**：所有面向人的文档用中文书写；代码、类型名、异常消息、命令保持原文。AI 助手的仓库约定见 [AGENTS.md](../AGENTS.md)。

## 与 `.claude/memory/` 的关系

`.claude/memory/` 下是给 AI 助手用的会话记忆（`MEMORY.md` + 若干 `project_*.md`），内容偏「实现细节速记」，不是面向人的文档。两者冲突时以本目录与代码为准。
