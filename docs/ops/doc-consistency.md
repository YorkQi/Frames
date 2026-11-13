---
title: 文档一致性检查
audience: 贡献者
status: stable
updated: 2026-11-13
sources:
  - scripts/check-docs.ps1
  - .githooks/pre-commit
  - .github/workflows/docs.yml
---

# 文档一致性检查

文档会腐烂。这一页说明我们用哪些**机械规则**守住它，以及规则拦不住什么。

原则：能机械验证的，不靠人记。凡是「数字 / 路径 / 链接 / 结构」类事实，都由脚本判定；只有「这句话是否说清了」留给评审。

## 1. 一条命令

```bash
powershell -NoProfile -File scripts/check-docs.ps1
```

- 默认检查：链接、锚点、front matter、manifest、程序集与 TFM、用例数声明、裸数字、表格形状、代码围栏、ADR 索引。
- `-VerifyTests`：额外运行 `dotnet test Frames.sln`，用 TRX 里的实际用例总数核对 `manifest.json` 的 `facts`。
- `-Quiet`：只输出失败项（hook 使用）。

失败输出形如 `D2 docs/README.md:45 -> 缺失目标 docs/guide/nope.md`，按编号查下表。

## 2. 规则清单

| 编号 | 规则 | 失败通常意味着 |
| --- | --- | --- |
| D1 | `docs/**/*.md` 每页都在 `manifest.json` 登记，且每条 path 真实存在 | 新页面没登记，或文件改名后没同步 manifest |
| D2 | 所有 md 的相对链接目标存在（跳过 http/https/mailto/锚点） | 移动/删除了文件，链接没跟着改 |
| D3 | 所有 `#锚点` 在目标文件中存在（按 GitHub 的 slug 规则） | 改了标题文字或编号，锚点失效 |
| D4 | 每页 front matter 齐全，且 `title`/`audience`/`status`/`updated` 与 manifest 一致；`sources` 路径存在 | 改了代码路径或漏写 front matter |
| D5 | `src/` 下每个 `Frame.*.csproj` 都在 manifest 的 `facts.assemblies` 与根 README、AGENTS 的程序集表中出现，且 TFM 与 csproj 一致 | 新增/删除程序集，或改了 TFM 没同步文档 |
| D6 | 用例总数在 `manifest.facts`、根 README、根 AGENTS、`ops/testing.md` 四处一致；`-VerifyTests` 时还要与实测一致 | 加了测试没更新数字 |
| D7 | 非声明位不得出现「N 个用例」；「N 个程序集」必须等于 `facts.assemblies` | 把会腐烂的数字复制进了别的页面 |
| D8 | 表格每行列数与表头一致；代码围栏成对 | 手工编辑破坏结构 |
| D9 | `docs/design/adr/*.md` 每篇都在 ADR 索引中出现 | 新增 ADR 没登记 |
| D10 | 同一文件内标题锚点唯一 | 两个 `## 4. 边界` 导致链接指向第一个 |

`facts` 是唯一数字源：

```json
"facts": { "assemblies": 6, "frameworkTestCases": 99, "sampleTestCases": 13, "totalTestCases": 112 }
```

## 3. 提交前自动跑

```bash
git config core.hooksPath .githooks
```

启用后，每次提交都会执行 `.githooks/pre-commit`：先跑 `check-docs.ps1 -Quiet`，失败则拒绝提交。hook 只做快速检查（不跑 `dotnet test`），几秒内结束。

CI 兜底见 `.github/workflows/docs.yml`：push / PR 时在 ubuntu 上跑 `-VerifyTests`，这一步才真正执行测试并核对用例数。本地 hook 是即时反馈，CI 才是合并门。

## 4. 改数字的标准流程

1. 改代码/测试；
2. 跑 `dotnet test Frames.sln`，记下总数；
3. 只改 `docs/manifest.json` 的 `facts`（以及 `updated`）；
4. 跑 `check-docs.ps1 -VerifyTests`，它会指出还有哪处声明不一致；
5. 不要手改散落在正文里的数字——正文里本来就不该有。

## 5. 拦不住什么

- **语义正确性**：`sources` 路径存在不代表那一段描述仍然准确；
- **代码示例可编译**：示例是片段，没有编译验证（`samples/` 里的完整代码会随 `dotnet build` 验证）；
- **术语一致**：同一概念在不同页面可能叫法不同；
- **优先级判断**：design-notes 里的技术债排序仍需人评审。

这些靠 review 与 [ADR](../design/adr/README.md) 兜底，不要指望脚本。
