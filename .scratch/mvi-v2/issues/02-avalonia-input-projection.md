# 02: Avalonia 表单输入与状态投影闭环

**What to build:** 一个真实 Avalonia 表单通过 State、Feature、View 三部分实现编辑、派生展示和按字段更新，无需手写绑定 ViewModel。

**Blocked by:** 01 — 独立核心消费者跑通纯状态闭环。

**Status:** completed

## Acceptance criteria

- [x] 真实 View 的输入通过生成入口进入 Model，显示值取自已提交快照；普通回写、附加纯规则、只读投影和模型可表达的中间输入均可运行。
- [x] 输入连接与字段投影采用平台本地绑定，不新增跨 Feature 观察通道或框架逐字段 R3 订阅。
- [x] 投影在正确 UI 线程、提交门外执行，版本单调；无关状态变化不刷新无关展示。
- [x] 默认合并待展示更新，显式需要中间展示时可选择相应调度方式；合并不吞掉业务输入。
- [x] 用户声明编译测试与真实 Avalonia 宿主验收同时通过，并提供后续表单可复用的最小演示。

## Verification

- TDD：消费者生成声明先因缺少 `Projection`/`CreateProjection` 失败，生成后编译、Emit 并执行通过。合并回调先因持续消费通知中的新输入失败，修复后每次回调只展示一个版本并归还 UI 消息泵。
- 公开 Feature/投影验证覆盖普通规则回写、只读字段、有关字段变化通知、100 次并发提交及单个调度回调、两种模式的展示版本、创建连接与 1000 次后台提交竞态、门外回调、释放重连、调度和通知异常恢复。用户声明消费测试覆盖生成成员保留名称诊断。
- 针对性 TUnit `/*/*/Projection*Tests/*`：14/14 通过；v2 测试项目和最新 Avalonia 样例 Release 构建均为 0 警告、0 错误。移除已不必要的展示范围公共协议后，相应保留名称诊断用例一并移除。
- 真实 Windows Avalonia 宿主 `--verify-v2-input` PASS；结果文件 `.scratch/v2-input-02-result.txt`。原生输入属性连接及只读 OneWay 绑定覆盖普通/规则回写、同值归一化、展示等待期间 A/B/A 输入全部提交、通知回调中的跨字段及同字段新输入、同字段通知内 A/B/A 全部提交、只读输入拒绝、释放输入连接、金额中间值、只读派生字段、无关/后台同值字段、UI 线程、500 次后台提交合并一次展示，以及 `EveryCommit` 的 A/B/C 和版本 1/2/3。通过原生控件属性触发输入，未宣称物理键盘覆盖。
- Spec 审查回归：真实宿主先证明原生 TwoWay 在 Ada→带空格 Ada 归一化场景保留原始控件值，`UpdateTarget` 亦无法纠正；公开投影输入反馈测试先红后绿。真实 A/B/A、跨字段通知回流、同字段通知内 A/B/A 先红后绿，排除按旧值或整个通知范围推断输入来源的方案。最终 `BindInput` 使用原生控件属性及事件组成本地连接，输出抑制仅在本连接实际 `SetCurrentValue` 调用的 try/finally 内有效；生成投影对本地编辑字段提供提交反馈，后台同值不通知。只读字段保留原生 OneWay 绑定，不再需要公共展示范围协议。输入 getter/setter 委托只在接线时创建，生成调用使用缓存静态委托。
- 最小演示：`sample/MiKiNuo.Mvi.Samples.Avalonia/Features/V2Input`，启动参数 `--v2-input`，原认证入口保持默认。

## Final validation

| 验证 | 最终结果 |
| --- | --- |
| 干净 Release 解决方案构建 | 通过，0 错误；5 条原有 v1 MVI0013 测试夹具警告，无新增警告 |
| 完整串行测试 | 270/270 通过；失败 0 |
| 生成声明与公开投影针对性测试 | 14/14 通过；包含消费声明编译、Emit、实际执行及并发回归 |
| 实际 Windows Avalonia 宿主 | HOST_EXIT=0、PASS；覆盖归一化、通知内输入回流、500 次提交合并 1 次展示及逐次中间展示 |
| 最终 Diff、格式与独立审查 | Diff 检查通过；12 份手写 C# 均为 UTF-8 BOM/CRLF；Standards 0、Spec 0 |

最终完整验证命令：

```powershell
rtk proxy dotnet clean MiKiNuo.Mvi.slnx -c Release
rtk proxy dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore
rtk proxy dotnet test --solution MiKiNuo.Mvi.slnx -c Release --no-build --max-parallel-test-modules 1 --results-directory TestResults --timeout 3m
```

## Spec coverage

- 用户故事：US01、US06、US07、US08、US10、US46、US50。
- 实现决策：D02、D07、D10、D25。
- 行为验收：T01、T20。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。
