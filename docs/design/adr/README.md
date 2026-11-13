---
title: 架构决策记录
audience: 贡献者
status: stable
updated: 2026-11-13
sources:
  - docs/design/adr
---

# 架构决策记录（ADR）

这里记录**已经生效、且改动代价较高**的技术选择。它回答「当时为什么这么选、放弃了什么」，不回答「怎么用」——用法在 `concepts/`。

## 索引

| 编号 | 决策 | 状态 |
| --- | --- | --- |
| [0001](0001-unitofwork-dispatchproxy.md) | 用 `DispatchProxy` 实现声明式工作单元 | accepted |
| [0002](0002-domain-events-after-commit.md) | 领域事件在事务提交后分发，从被追踪实体收集 | accepted |
| [0003](0003-auditing-in-savechanges.md) | 审计字段在 EF `SaveChanges` 前统一填充 | accepted |
| [0004](0004-samples-single-composition-root.md) | 示例采用单一组合根 `AddTaskBoard` + 多宿主 | accepted |

## 什么时候写一篇

满足任意两条就写：

- 影响公共 API 或程序集依赖方向；
- 有至少一个被认真考虑过的备选方案；
- 后来者可能想「改回去」，而不知道当初的代价；
- 决策引入了已知限制（需要写进 [design-notes](../design-notes.md)）。

## 模板

```markdown
# ADR NNNN 标题

- 状态：accepted | superseded by NNNN | deprecated
- 日期：YYYY-MM-DD

## 背景
约束、问题、触发事件。

## 决策
做了什么，一句话说清。

## 备选与取舍
每个备选：为什么不选，代价是什么。

## 后果
得到什么、失去什么、必须遵守什么。
```

## 规则

- 一篇只记一个决策；已接受的 ADR 不改正文，被推翻时新建一篇并把旧文状态改为 `superseded by NNNN`。
- 新增后在本页索引登记，校验脚本会检查索引与实际文件一致。
- 涉及的行为边界同步写进 [design-notes](../design-notes.md)，两处不要互相矛盾。
