# 06: 有界 Queue 顺序处理业务操作

**What to build:** 独立消费者按声明顺序执行一组导出或保存作业，队列达到容量时得到明确拒绝，并能等待每个作业的真实处理结果。

**Blocked by:** 03 — 异步 Operation 的验证、反馈与完成契约。

**Status:** completed

## Acceptance criteria

- [x] Queue 由业务明确选择并有有限容量；验证并拒绝无效容量配置，不创建隐式无界工作积压。
- [x] 已接纳作业按契约顺序启动；每次实际启动的验证与交给 IO 的输入快照一致，不能用旧验证授权新输入。
- [x] 接纳、排队与业务完成可区分；满载拒绝不调用服务，排队中取消的作业不会后来悄悄启动。
- [x] 一个作业的业务失败或故障不会使其他已接纳作业静默消失；每项都有可判定结果与正确运行状态。
- [x] 通过公开操作入口和可控服务证明顺序、满载、取消、故障及并发状态更新，复用实例运行协议。

## Spec coverage

- 用户故事：US13、US14、US18、US20。
- 实现决策：D08、D11、D12、D13、D15。
- 行为验收：T03、T04、T05、T06。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## 实现与验收记录

实现提交 `f4dcfba` 已合入当前 v2 分支，并在 `2b16a87` 与 Latest、Parallel、Mediator 统一整合。本票此前的 ready-for-agent 为未同步状态，2026-10-02 按已合入实现和当前验证更新。

公开边界回归位于 `test/MiKiNuo.Mvi.V2.Tests/OperationQueueTests.cs` 与操作声明测试。当前合入基线完整串行测试 433/433，切片 09 后完整 v2 测试 213/213，失败和跳过 0。

本次实际执行 Release 无界面消费者，退出码 0，`Headless queue loop PASS`：等待容量有界、FIFO 启动、实际启动时验证和采样当前输入、分别返回结果且完成状态一致。命令：`rtk proxy test/MiKiNuo.Mvi.Headless.Consumer/bin/Release/net10.0/MiKiNuo.Mvi.Headless.Consumer.exe`。同次运行的 state、operation、mediator 闭环也全部 PASS。

实例关闭后取消等待项、不合作 IO 的释放边界由后继票 10 与完整集成票 20 扩展验证。
