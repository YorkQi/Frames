---
title: 文档目录约定
audience: AI 助手
status: stable
updated: 2026-11-13
sources:
  - scripts/check-docs.ps1
  - docs/manifest.json
---

# docs/ 写作约定

这一页只讲「在 docs/ 里放什么、怎么写、怎么验」。仓库级约束见 [../AGENTS.md](../AGENTS.md)。

## 1. 四层分工

| 目录 | 回答 | 不写什么 |
| --- | --- | --- |
| `guide/` | 我该怎么做（步骤、命令、最小代码） | 机制推导、历史决策 |
| `concepts/` | 它是什么、边界在哪、为什么这样工作 | 完整教程、待办清单 |
| `design/` | 当时为什么这样选、代价是什么、还剩什么债 | 使用步骤 |
| `ops/` | 怎么验证、怎么自动化 | 功能说明 |

判定方法：如果一句话以「你先…然后…」开头，它属于 `guide/`；以「因为…所以…」开头，属于 `design/`；以「它保证/不保证…」开头，属于 `concepts/`。

## 2. 每页必须有 front matter

```yaml
---
title: 页面标题（与 H1 一致）
audience: 使用者 | 贡献者 | AI 助手 | 全部
status: stable | evolving | accepted | deprecated
updated: YYYY-MM-DD
sources:            # 这一页描述的事实来自哪些代码路径；必须真实存在
  - src/Frame.Core/Uow
---
```

`sources` 是「以代码为准」的机械锚点：校验脚本会逐个检查路径是否存在。改代码导致路径变化时，同步改这里。

## 3. 新增一页的流程

1. 在对应目录建 `.md`，写 front matter 与 H1；
2. 在 [manifest.json](manifest.json) 的 `pages` 里登记（path 必须与磁盘一致，含 `docs/` 前缀）；
3. 在 [README.md](README.md) 的页面清单加一行；
4. 如涉及新决策，另加一篇 `design/adr/NNNN-*.md` 并在 [ADR 索引](design/adr/README.md) 登记；
5. 跑 `powershell -NoProfile -File scripts/check-docs.ps1`。

## 4. 写作规则

- **中文优先**；类型名、方法名、异常消息、命令保持原文。
- **签名照抄源码**：参数顺序、返回类型（`Task<TEntity>` 不是 `Task<TEntity?>`）、可空性都要对。
- **状态标记**：有边界的段首标 `⚠️ 限制`；未接线的标 `⚠️ 未接线` 并链到 [design-notes](design/design-notes.md)。
- **数字不裸写**：用例数、程序集数只允许出现在 [manifest.json](manifest.json) 派生出的声明位（根 README、根 AGENTS、[ops/testing.md](ops/testing.md)）。其余页面写「见测试说明」，不要复制数字。
- **链接用相对路径**，锚点必须与目标标题一致（中文标题的锚点形如 `#4-延迟持久化`）。
- 一页只回答一类问题；超过约 400 行时按「机制 / 配方 / 决策」拆页。

## 5. 校验

```bash
powershell -NoProfile -File scripts/check-docs.ps1              # 链接、锚点、front matter、事实、表格
powershell -NoProfile -File scripts/check-docs.ps1 -VerifyTests # 另外执行 dotnet test 并核对用例数
```

失败时按输出里的 `文件:行` 定位；规则明细见 [ops/doc-consistency.md](ops/doc-consistency.md)。
