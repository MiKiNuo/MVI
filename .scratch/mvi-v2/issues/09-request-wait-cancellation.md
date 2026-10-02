# 09: 取消请求等待与目标执行分别生效

**What to build:** 调用者可以停止等待一个已接纳的跨 Feature 请求，而目标按默认归属继续完成；契约可显式选择传播协作取消。

**Blocked by:** 08 — 独立 Feature 通过 Mediator 定向请求。

**Status:** completed

## Acceptance criteria

- [x] 接纳前取消的请求不启动目标业务；接纳后默认由目标管理执行。
- [x] 取消调用方等待形成 WaitCanceled，不能被误报为正常业务响应、目标完成或业务撤销。
- [x] 显式传播取消按契约作用于目标执行，并与调用者等待是否结束分别表达。
- [x] 目标已提交的状态或已发生的外部操作不因发起者停止等待而回滚；仍在执行的目标工作持续受跟踪。
- [x] 可控服务验证接纳前取消、接纳后取消等待、目标迟后成功或失败、显式传播和取消与完成竞争。

## Implementation and acceptance evidence

2026-10-02 完成，依据 ADR 0022。

- `CreateRequestPort` 通过 `RequestCancellationPolicy` 声明执行归属：默认 `TargetOwned` 忽略发送者的执行令牌；`Propagate` 接受发送者显式提供的 `executionCancellationToken`，非法策略立即拒绝。
- `Mediator.SendAsync` 分别提供 `waitCancellationToken` 和 `executionCancellationToken`。仅取消等待时返回 `WaitCanceled` 且没有操作结果；仅取消执行并继续等待时返回 `Responded` 和目标的 `Canceled` 操作结果。希望一个令牌控制两者时须显式传入两处。
- 执行令牌复用 `DispatchOperation` 和既有操作内核。没有新增取消注册、链接源、队列或等待代理；Queue 注册、Latest 链接源与受跟踪工作继续遵循现有真实退出屏障。
- `MediatorRequestTests` 使用 TCS 控制服务与受跟踪工作，覆盖等待预取消、两种寻址的执行预取消、默认和支持传播端口的晚成功/晚故障、已提交状态保留、等待结束后继续传播、Reject/Latest 真实退出、独立等待/执行取消、取消与完成竞争、响应先结束及非法策略。
- `MediatorOperationPolicyTests` 覆盖 Queue 等待取消后保留 FIFO 工作、显式执行取消归还排队容量且不启动被取消服务、Latest 不合作服务真实退出后才完成取消，以及原有 Queue/Latest/Parallel 混合入口回归。

验证：

1. RED：`rtk proxy test/MiKiNuo.Mvi.V2.Tests/bin/Release/net10.0/MiKiNuo.Mvi.V2.Tests.exe --maximum-parallel-tests 1 --treenode-filter "/*/*/*/PreCanceledExecutionTokenOnlyStopsOptedInTarget*"` — 保留 API 骨架但仍传 `None` 时 4 项中 2 项失败（传播端口预取消被错误报告为 Completed），退出码 2。
2. GREEN：同一命令在接入执行令牌后 4/4 通过，退出码 0。
3. 针对性回归：`rtk proxy test/MiKiNuo.Mvi.V2.Tests/bin/Release/net10.0/MiKiNuo.Mvi.V2.Tests.exe --maximum-parallel-tests 1 --treenode-filter "/*/*/Mediator*/*"` — 45/45 通过；之后新增的两个 Latest 等待结束变体由下面完整运行覆盖。
4. 最终编译：`rtk proxy dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false` — 退出码 0，0 错误，5 条与基线一致的 v1 `MVI0013` 警告。
5. 最终完整 v2 回归：`rtk proxy test/MiKiNuo.Mvi.V2.Tests/bin/Release/net10.0/MiKiNuo.Mvi.V2.Tests.exe --maximum-parallel-tests 1` — 213/213 通过，0 失败、0 跳过，退出码 0，包含最终 47 项 Mediator 用例。
6. `rtk proxy git diff --check` — 退出码 0；修改的 C# 文件保持 UTF-8 BOM 和 CRLF。

完整日志位于 `.scratch/mvi-v2/09-red-test.log`、`09-green-test.log`、`09-focused-tests.log`、`09-solution-build.log` 和 `09-full-v2-tests.log`。取消为协作语义，不合作工作会继续占有运行资源直至真实退出；这是已验收的契约边界。

## Spec coverage

- 用户故事：US13、US14、US27、US36。
- 实现决策：D12、D15、D19、D20。
- 行为验收：T05、T10。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。
