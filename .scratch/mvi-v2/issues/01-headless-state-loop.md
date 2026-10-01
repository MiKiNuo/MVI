# 01: 独立核心消费者跑通纯状态闭环

**What to build:** 无 GUI 消费者只声明 State 与 Feature，即可通过生成的强类型输入入口改变并读取不可变快照，验证 v2 最小 MVI 路径可实际编译执行。

**Blocked by:** None (can start immediately).

**Status:** completed

## Acceptance criteria

- [x] 首先完成使该消费场景可落地的最小构建预调整：隔离 v1 与 v2 编译资产和分析器适用范围，保留有效的 v1 验证；遵守既有源码、示例、测试目录布局。
- [x] 普通可编辑字段只声明一次；附加纯转换按有关输入调用；只读字段不生成可写入口。作者无需手写 Intent、Dispatcher 或 ViewModel。
- [x] 输入进入实例统一入口，纯转换产出并提交 RuntimeSnapshot；业务 State、空或已有操作状态与单调 Version 来自一致快照，纯规则故障保留原快照。
- [x] 独立无 GUI 消费程序能构造两个实例并通过公开入口验证状态隔离；生成器作为编译资产使用，运行时不依赖生成器或 UI 框架。
- [x] 用户声明编译、生成入口行为和错误输入声明都有针对性测试；临时并行构建仅服务渐进迁移，不建立长期 v1 兼容 API。

## Spec coverage

- 用户故事：US01、US04、US06、US07、US10、US22、US51。
- 实现决策：D01、D02、D05、D06、D07、D10、D27、D29。
- 行为验收：T01、T19。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Implementation and acceptance evidence

- 新增独立核心 `src/MiKiNuo.Mvi` 和编译期项目 `src/MiKiNuo.Mvi.Generators`，通过显式项目引用隔离 v1/v2；全局构建配置、v1 源码和测试保持原有实现。四个新增项目均位于既有 src/test 目录。
- `[Input]` 生成强类型 `Set<Property>`；`[OnInput]` 只替换有关输入的默认回写。内部值类型 Intent 经 Reducer 和单实例 Store 原子提交 `RuntimeSnapshot`，包含 State、单调 Version 和当前始终为空的不可变 OperationStates。异常、null 结果及同实例重入不提交快照。
- 独立消费者位于 `test/MiKiNuo.Mvi.Headless.Consumer`，两个实例的状态隔离验证成功。实际运行输出 `Headless state loop PASS`；运行依赖清单只有消费者和核心，没有生成器、UI、R3 或 DI。

| 验证 | 最终结果 |
| --- | --- |
| 干净 Release 解决方案构建 | 通过，0 错误；5 条原有 v1 MVI0013 测试警告，无新增警告 |
| 审查修正后的 Release 解决方案构建 | 通过，增量构建 0 警告、0 错误 |
| 完整串行测试 | 256/256 通过：v1 216 项、v2 40 项；失败 0、跳过 0 |
| 声明与增量回归 | 合法入口可编译；错误定位原声明；无关 Feature 输出 Cached/Unchanged，单 Feature 修改仅有关输出 Modified |
| 最终 Diff 与格式 | 仅当前工单范围；手写 C# 使用 UTF-8 BOM/CRLF，Diff 检查通过 |

最终完整测试命令：

```powershell
rtk proxy dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore
rtk proxy dotnet test --solution MiKiNuo.Mvi.slnx -c Release --no-build --max-parallel-test-modules 1 --results-directory TestResults --timeout 3m
```

## Standards

原有 14 个公开测试方法及新增 3 个公开回归方法已补齐中文 XML 文档。独立规范复核：0 项遗留规范问题、0 项代码气味。

## Spec

独立规格审查发现的重复 partial 生成、入口与类型同名冲突、外部记录隐藏可变存储三类问题均已修复，并通过先失败后通过的回归测试验证。外部记录问题使用真实 Roslyn Emit 独立程序集复现，覆盖直接、嵌套及继承路径。规格复核：0 项遗留问题、0 项新增发现。

## Supported declaration boundary

Feature 当前要求顶层、非泛型 partial 类，直接继承 `Feature<TState>`。记录状态须在消费者源码中声明，递归验证其成员及继承链；不依赖外部记录的公开元数据猜测私有存储是否不可变。明确支持的 BCL 标量及不可变集合继续可用，其他未验证类型以 MVI2002 拒绝。纯规则和计算 getter 的方法体遵守作者纯度契约，本票不声称自动证明任意方法体纯度。Operation、平台投影及正式 NuGet 消费按后续工单交付。
