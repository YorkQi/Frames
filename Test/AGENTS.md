# Test/

两套测试：`Test/TestUnit`（框架行为）与 `samples/TaskBoard/Tests`（示例领域模型）。

## 分工

| 位置 | 测什么 | 不测什么 |
| --- | --- | --- |
| `Test/TestUnit` | 框架契约与行为：实体相等性、UOW 处理器、代理、审计填充、模块排序、分页/参数/连接串、枚举与选项 | 真实数据库、Redis、HTTP 宿主 |
| `samples/TaskBoard/Tests` | 聚合不变量与领域事件（`Project.Create/Rename/Archive`、`TaskItem.Complete`） | 框架行为、持久化 |

## 约定

- xUnit 2.7.0，`net8.0`；`TestUnit` 引用全部 `src` 项目，`TaskBoard/Tests` 只引用该示例的 `Domain`。
- `Frame.Core` 已开 `[assembly: InternalsVisibleTo("TestUnit")]`；测内部类型（`UnitOfWork`、`SortModulesByDependency`）不需要改可见性。
- 用例名用「方法_场景_预期」；一个用例只断言一件事；时间相关用固定 `IClock` 实现，不依赖 `DateTime.UtcNow` 漂移。
- 新增框架公共行为必须补测试；只改文档不用补。
- 测试数量是声明位事实：改动后只改 `docs/manifest.json` 的 `facts.frameworkTestCases` / `sampleTestCases` / `totalTestCases`，再跑 `check-docs.ps1 -VerifyTests`。

## 运行

```bash
dotnet test Frames.sln                          # 全部
dotnet test Test/TestUnit/TestUnit.csproj       # 只跑框架
dotnet test samples/TaskBoard/Tests/Tests.csproj
```

不要在没有实际运行的情况下声称「测试通过」。

文档：[测试与验证](../../docs/ops/testing.md)
