# 参与贡献

感谢你对 Frames 的关注。这份文档说明如何搭环境、跑测试、提改动，以及文档和代码的约定。

## 1. 环境

| 组件 | 版本 |
| --- | --- |
| .NET SDK | 8.0（`Frame.Databases`、`samples/*`、`Test/*` 为 `net8.0`） |
| 框架类库目标 | `netstandard2.1`（`Frame.Core`、`Frame.EventBus`、`Frame.Locks`、`Frame.Redis`、`Frame.Scheduler`） |
| 数据库（可选） | MySQL 8.x（示例与集成测试） |
| Redis（可选） | 6.x+（锁与缓存示例） |

```bash
git clone <repo>
cd Frames
dotnet restore
dotnet build
dotnet test Test/TestUnit
```

（示例需要先补 `appsettings.json` 的连接串，见 [docs/samples.md](docs/samples.md#21-先补配置)。）

## 2. 目录约定

```
src/            框架类库（每个模块一个项目，互不引用实现细节）
samples/        示例（只引用 src，不反向被引用）
Test/           测试（TestUnit 引用 src）
docs/           文档（与代码同步维护）
```

- 新增框架能力放进对应 `src/Frame.*` 项目；跨模块依赖只允许 Core ← 其他模块。
- `samples` 和 `Test` **不得**被 `src` 引用。
- 公共 API 必须有 XML 注释；`internal` 的实现细节不要泄漏到公共签名。

## 3. 代码风格

- 文件级 `namespace`（本仓库统一使用 file-scoped namespace）；
- 私有字段 `_camelCase`；常量与静态只读字段按仓库现有写法；
- 异步方法以 `Async` 结尾并接受 `CancellationToken cancellationToken = default`；
- 不引入新的第三方依赖，除非该模块的 README 与 `CHANGELOG` 同步说明理由；
- 保持 TFM 不变：给 `netstandard2.1` 项目加 `net8.0` 独有的 API 会破坏兼容性。

## 4. 提交与 PR

1. 一个 PR 只做一件事（一个特性、一个修复或一组文档）；
2. 提交信息用中文或英文均可，建议 `模块: 动作` 格式，例如 `Databases: 修复方言表名缓存`；
3. PR 描述里写清：**动机**、**行为变化**、**如何验证**（命令与预期输出）；
4. 行为变化必须同步 `CHANGELOG.md` 的 `[Unreleased]` 段；
5. 涉及公共 API 的改动，同步更新 `docs/` 下对应页面与根 `README.md` 的能力表。

## 5. 新增一个模块

1. 在 `src/` 下建 `Frame.Xxx` 项目，按需选择 TFM（能在 `netstandard2.1` 上跑就不要用 `net8.0`）；
2. 提供 `UseXxx(this ServiceConfigurationContext, ...)` 扩展方法，只注册自己的服务，不假设宿主；
3. 需要生命周期钩子时，实现 `FrameModule` 并在 `[DependsOn]` 中声明依赖；
4. 在 `samples/` 加最小可运行示例；
5. 在 `Test/TestUnit` 加纯逻辑测试（能脱离数据库的部分必须覆盖）；
6. 在 `docs/` 加一页说明，并在 `docs/README.md`、根 `README.md` 的模块表中登记。

## 6. 文档约定

- **以代码为准**：写「支持 X」之前先在源码里确认；不确定就写「当前未接线」并链到 [docs/design-notes.md](docs/design-notes.md)。
- **每个代码块可编译**：示例里的类型名、方法名、参数必须与源码一致（例如 `InsertBatchAsync(IEnumerable<TEntity>, CancellationToken)` 不要写成 `AddRange`）。
- **标注限制**：任何「看起来应该自动发生但其实没有」的行为，都要在文档里显式写出来，避免使用者踩坑。
- 文档之间用相对路径互链，新增页面后更新 [docs/README.md](docs/README.md) 的索引。

## 7. 报告问题

提 issue 时请附上：

- 复现步骤与最小代码；
- 期望行为与实际行为；
- `dotnet --info` 的关键行、相关模块版本；
- 若是数据库/Redis 相关问题，附连接串的**脱敏**形式与数据库版本。

## 8. 许可

提交即表示同意以仓库的 MIT 许可证发布你的贡献。
