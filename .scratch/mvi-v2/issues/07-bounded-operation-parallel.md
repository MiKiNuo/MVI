# 07: 有界 Parallel 独立完成并行业务

**What to build:** 独立消费者显式并行执行一组业务操作，限制同时执行数量，并分别获得结果而不串用状态和取消身份。

**Blocked by:** 03 — 异步 Operation 的验证、反馈与完成契约。

**Status:** completed

## Acceptance criteria

- [x] Parallel 由业务明确选择并声明有限并行度；非法配置与超过接纳能力的调用有明确结果。
- [x] 每次执行拥有独立身份、输入快照和结果；单个执行失败或取消不错误取消其他有效执行。
- [x] 所有状态转换仍按实例统一提交；完成一个执行不能把仍有其他有效执行的操作展示为空闲。
- [x] 不同执行应用结果时基于当前 State，不用各自旧快照整体覆盖；业务冲突条件由纯规则或业务服务表达。
- [x] 公开消费者测试覆盖并行度上限、乱序结束、单项取消、单项故障和状态一致性，并提供可运行演示。

## Spec coverage

- 用户故事：US13、US14、US19、US20。
- 实现决策：D06、D08、D11、D12、D13、D15。
- 行为验收：T03、T04、T05、T06。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## 实现与验收记录

2026-10-02，在独立分支 `codex/mvi-07-bounded-parallel` 完成。实现与审查固定基线为任务 03 提交 `0d70b53507a543b9a063c71c1b204f7943de5f8c`。受管工作树为 `C:\Users\luoji\.codex\worktrees\mvi-07-bounded-parallel\MVI`。

- 使用 `[Operation(Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]` 显式声明有限并行度；默认仍为 Reject。非法声明定位为 MVI2010，直接启动入口返回 Rejected/InvalidConcurrency，容量已满返回 Rejected/ConcurrencyLimitReached。
- 每次执行保留独立身份、通过验证的开始快照、取消令牌、所属工作与强类型结果。RuntimeSnapshot 使用不可变 RunningIds，RunningCount/IsRunning 从集合派生；兼容 RunningId 表示最早接纳且尚未退出的执行。
- 准入、验证和快照采样位于同一短提交门中，IO 位于门外。拒绝、验证失败和预取消保留已有身份；反馈以当前 State 执行纯转换，单项完成仅移除自己的身份。
- 已登记的子工作和嵌套工作真正退出之前继续占用并行容量；取消或故障不会提前释放该名额或回滚已提交状态。

## 验证

纯任务 03 基线首先复现 RED：第二次公开调用立即被默认策略拒绝；接入 Parallel 后通过 GREEN。新增 19 个公开边界用例（Parallel 行为 13 个、非法声明 6 个），覆盖竞争准入、乱序结束、独立输入和结果、旧快照不可变、输入保留、取消/故障隔离和真实退出屏障。

Release 解决方案 restore/build 成功，0 错误；5 条 MVI0013 警告来自未修改的既有 v1 测试夹具。完整测试 **337/337 通过**（v1 216、v2 121），失败和跳过均为 0；Operation 定向回归 **67/67 通过**。两个测试项目继续使用 `--maximum-parallel-tests 1`。

独立消费者默认模式与 `--parallel` 模式均实际运行，退出码 0。并行演示确认上限 2、第三次明确拒绝、部分完成时 RunningCount 为 1、最终 Total 为 10 且 RunningCount 为 0。运行证据保存在工作树 `.scratch/v2-parallel-07-result.txt`，复制命令见 `test/MiKiNuo.Mvi.Headless.Consumer/README.md`。

```powershell
dotnet run --project C:\Users\luoji\.codex\worktrees\mvi-07-bounded-parallel\MVI\test\MiKiNuo.Mvi.Headless.Consumer -c Release -- --parallel
```

## Standards

独立规范复核：**0 项遗留发现**，硬规范违约 0，需修改的 Fowler 异味判断 0。改动保留独立 Store、纯 Reducer、统一 RuntimeSnapshot 和门外 Effect，公开声明、生成器、运行时、测试与演示均在本票范围。7 个触及的 C# 文件已验证 UTF-8 BOM 和 CRLF；差异空白检查通过。

## Spec

独立规格复核：**0 项实质发现，0 项遗留**；缺失/部分实现 0，超出需求 0，错误契约 0。所有准入、拒绝、反馈与完成路径使用完整身份集合；Track 屏障观察新增嵌套工作，确认真实退出后再释放容量；生成器与运行时的配置规则一致。复核直接沿用已确认的测试与运行证据。

## 分支与整合范围

本票在共享 checkout 存在其他任务并发写入时转入独立工作树，当前验收范围为任务 03 基线加任务 07。提交位于 `codex/mvi-07-bounded-parallel`，尚未合并到 `codex/mvi-v2`。后续合并 Latest、Queue 等独立任务时，需要统一 OperationConcurrency 枚举值和生成器的策略验证；不能直接拼接各分支的数值判断。本树只实现 Reject 与有界 Parallel。
