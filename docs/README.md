---
title: 文档索引
audience: 全部
status: stable
updated: 2026-11-13
sources:
  - docs/manifest.json
---

# 文档索引

这是 Frames 的文档地图。**先看你属于哪一类，再只读那一列**——不需要从头读到尾。

根目录 [README.md](../README.md) 只回答「这是什么、怎么在 60 秒内跑起来」；所有细节、边界与决策都在这里。

## 三条入口

| 你是 | 从这里开始 | 之后按需 |
| --- | --- | --- |
| **第一次接触，想跑起来** | [快速开始](guide/getting-started.md) | [示例工程](guide/samples.md) → [常见任务配方](guide/recipes.md) |
| **要在业务项目里用** | [整体架构](concepts/architecture.md) | [核心模块](concepts/core.md) → [工作单元](concepts/unit-of-work.md) → [数据库](concepts/databases.md) → 按需查 `concepts/` 下的横切能力 |
| **要改框架代码** | [模块与目录结构](concepts/modules.md) | [设计说明](design/design-notes.md) → [ADR](design/adr/README.md) → [测试](ops/testing.md) → [贡献指南](../CONTRIBUTING.md) |

只想确认「某能力到底能不能用」：看 [设计说明](design/design-notes.md) 的「设计目标 vs 现状」表，那一页是唯一的事实清单。

## 目录怎么分

```
docs/
├─ README.md          你在这里：地图
├─ AGENTS.md          本目录的写作与校验约定（给智能体/贡献者）
├─ manifest.json      页面清单与事实数字的唯一来源，校验脚本据此检查
├─ guide/             上手与任务配方：怎么用
├─ concepts/          机制与 API：为什么这样、边界在哪
├─ design/            设计决策与落差：design-notes + ADR
└─ ops/               测试与文档自动化：怎么验证
```

一个页面只回答一类问题。新增内容时先判断它属于 `guide`（怎么做）、`concepts`（是什么/为什么）、`design`（当时为什么这么选）还是 `ops`（怎么验），不要把四种混进一页。

## 页面清单

| 页面 | 回答的问题 |
| --- | --- |
| [guide/getting-started.md](guide/getting-started.md) | 需要什么环境？最小可运行工程长什么样？启动报错怎么排查？ |
| [guide/recipes.md](guide/recipes.md) | 加一个 CRUD 端点、发领域事件、做软删除、加锁、加定时任务，各要改哪几处？ |
| [guide/samples.md](guide/samples.md) | 三个示例怎么跑？TaskBoard 怎么分层、怎么共用组合根？表结构在哪？ |
| [concepts/architecture.md](concepts/architecture.md) | 代码怎么分层？程序集怎么被发现？服务生命周期？一次请求经过哪些环节？ |
| [concepts/modules.md](concepts/modules.md) | 每个程序集有哪些目录？模块怎么被发现/排序？生命周期钩子何时执行？ |
| [concepts/core.md](concepts/core.md) | `Entity` 的相等性？仓储契约？UOW 怎么嵌套与回滚？ |
| [concepts/unit-of-work.md](concepts/unit-of-work.md) | `[UnitOfWork]` 怎么生效？提交/回滚边界？为什么嵌套不重复提交？ |
| [concepts/domain-events.md](concepts/domain-events.md) | 事件何时收集、何时分发？会不会丢？处理器要幂等吗？ |
| [concepts/auditing.md](concepts/auditing.md) | 审计字段谁填？怎么换时间/用户来源？软删除有什么后果？ |
| [concepts/databases.md](concepts/databases.md) | EF Core 与 Dapper 如何共存？为什么插入返回 0？分页 SQL 怎么生成？ |
| [concepts/locks.md](concepts/locks.md) | 本地锁和 RedLock 的区别？拿不到锁会怎样？ |
| [concepts/eventbus.md](concepts/eventbus.md) | 事件如何投递？为什么串行？重试几次？会丢吗？ |
| [concepts/scheduler.md](concepts/scheduler.md) | 两种注册方式？Cron 几段？作业实例从哪来？重启后还在吗？ |
| [concepts/redis.md](concepts/redis.md) | 支持哪些数据结构？多路复用器何时创建/关闭？ |
| [design/design-notes.md](design/design-notes.md) | 哪些设计与实现不一致？技术债的优先级？ |
| [design/adr/README.md](design/adr/README.md) | 关键决策当时为什么这么选？备选是什么？ |
| [ops/testing.md](ops/testing.md) | 测试覆盖了什么、没覆盖什么、怎么补？ |
| [ops/doc-consistency.md](ops/doc-consistency.md) | 文档怎么自动校验？hook 和 CI 怎么跑？ |

## 维护约定

1. **以代码为准**：文档描述当前代码的行为。冲突时改文档，并在 [design-notes.md](design/design-notes.md) 记录仍未修的实现落差。
2. **区分「已实现」与「设计意图」**：有边界的标 `⚠️ 限制`；未接线的标 `⚠️ 未接线`，并链到 design-notes。
3. **签名照抄代码**：类型名、方法名、参数顺序、异常消息与源码一致，便于检索。
4. **改动同步三处**：对应页面、`CHANGELOG.md`、根 README 的能力表；事实数字只改 [manifest.json](manifest.json)，再跑校验。
5. **中文优先**：代码、类型名、异常消息、命令保持原文。写作细则见 [AGENTS.md](AGENTS.md)。

文档一致性由 `scripts/check-docs.ps1` 机械检查（链接、锚点、front matter、事实数字、表格形状），规则与流水线见 [ops/doc-consistency.md](ops/doc-consistency.md)。

## 与 `.claude/memory/` 的关系

`.claude/memory/` 是早期 AI 会话速记，可能过时；它不是文档来源，冲突时以本目录与代码为准。目录约定见 [.claude/memory/AGENTS.md](../.claude/memory/AGENTS.md)。
