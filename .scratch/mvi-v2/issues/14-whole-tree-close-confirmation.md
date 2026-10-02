# 14: 整个组合确认关闭并抵抗状态变化

**What to build:** 关闭带未保存内容的工作区时先确认全部相关子实例；任一拒绝则整体继续使用，确认期间发生的状态或成员变化必须重新验证。

**Blocked by:** 13 — 动态工作区组合独立子 Feature。

**Status:** completed

## Acceptance criteria

- [x] 关闭准备覆盖整个所有权范围，准备阶段不提前取消其他子实例 IO 或释放其资源。
- [x] 任一拒绝时所有尚未关闭实例保持可用，输入、操作和定向路由继续正常工作。
- [x] 记录与确认相关的状态和成员条件；编辑、新增、移除或其他相关变化使旧确认失效并重新验证。
- [x] 最终关闭在条件仍有效且成员关系稳定时提交，之后才执行不可撤销的取消与释放流程，不能逐个关闭后才发现另一个拒绝。
- [x] 用真实工作区演示和可控确认信号覆盖全部同意、单子项拒绝、准备失败、并发编辑、成员变动及等待取消。

## Implementation and acceptance evidence

2026-10-02 实现与验证，基线 `39dc57c`；架构/并发独立审查由 Root 执行，不扩切片15导航依赖链。

- `Feature.RequestCloseAsync(token)` 返回 `CloseRequestResult` 的 Closed/Rejected/WaitCanceled/Faulted；Closed含原唯一CloseResult/Ticket。同步Close保持无条件清理，工厂失败不询问业务。
- 强类型protected `ConfirmCloseAsync(CloseConfirmation<TState>)` 默认允许；上下文提供不可变准备Snapshot、协作令牌与Track，不能直接Update业务State。
- 准备及最终提交均树Gate→按InstanceId固定顺序全部ModelGate，短锁捕获/校验每层Revision与业务StateVersion，所有确认await、取消、释放与平台回调在门外。条件改变重新完整准备，新增成员不继承旧同意。
- StateVersion按 `EqualityComparer<TState>.Default` 逻辑变化递增，支持struct与不可变record；Operation Busy/完成及新引用但相等State不制造重验饥饿。
- 确认活动复用Operation的CTS、activeExecutions、Track、取消回调与真实清理协议，登记先于调度；不写用户RuntimeSnapshot伪操作。取消等待或另一个Close不能提前释放仍使用Scope的确认IO/子工作；直接Released自等待保护仍生效。
- 任一拒绝/故障/等待取消不因该请求关闭/解绑/取消其它业务IO；最终条件有效时一次Commit整树后锁外finish。并发批准复用原Ticket，提交赢后取消不能伪报未关闭。
- 工作区Editor含未保存文本业务确认，默认保留dirty编辑。Window.Closing取消原生关闭并await请求，仅批准后设置标志再次Close；普通移除先请求确认，批准才卸载成员。
- 公开TCS矩阵覆盖同意/拒绝/故障、拒绝后的输入/Send/Post、编辑/Add/Remove嵌套重验、Busy/struct/相等record不重问、合法确认回调结构变化、取消等待后不合作IO与Track范围保留、自Released保护、并发唯一票据与迟后取消。

实际验证：

1. V2测试项目Release构建 `--no-restore -p:UseSharedCompilation=false` — exit 0，0警告、0错误。
2. V2可执行程序 `--maximum-parallel-tests 1 --treenode-filter "/*/*/CloseConfirmationTests/*"` — **11/11**，exit 0。
3. V2完整运行 `--maximum-parallel-tests 1` — **280/280**，0失败、0跳过，exit 0。
4. Avalonia样例/平台Release构建 — exit 0，0警告、0错误。
5. `rtk proxy sample/MiKiNuo.Mvi.Samples.Avalonia/bin/Release/net10.0/MiKiNuo.Mvi.Samples.Avalonia.exe --verify-v2-close --v2-close-result=F:/MiKiNuoProjects/MVI/.scratch/mvi-v2/14-close-result.txt` — **PASS v2-close**，真实Windows HWND/UI线程、原生Closing拒绝保留窗口/输入/IO、确认期间编辑/新增后重验、批准才关闭，自动退出exit 0。
6. 解决方案Release构建 — exit 0，0错误、5条既有v1 MVI0013警告。
7. 原有/认证样例测试可执行程序 `--maximum-parallel-tests 1` — **239/239**，0失败、0跳过，exit 0。
8. `rtk proxy git diff --check` — exit 0，C#保持CRLF、UTF-8 BOM。

结果文件 `.scratch/mvi-v2/14-close-result.txt`；完整日志 `14-build.log`、`14-tests.log`、`14-full-v2.log`、`14-sample-build.log`、`14-close-run.log`、`14-solution-build.log`、`14-original-tests.log`。Root负责独立审查与提交。

## Independent review boundary corrections

2026-10-02 三P2公开RED→GREEN：

- 实际正在确认的child被Remove/Close：旧实现把guard取消误报caller WaitCanceled，公开RED exit 2。当前caller令牌未取消且root未闭时重新完整准备剩余树。
- A未合作确认等待、B先批准提交RootClosed、随后取消A：旧catch误报WaitCanceled，RED exit 2。统一短树门判定 `ReadInterruption` 先返回已闭原CloseResult，否则才按caller token返回WaitCanceled；A/B共享票据，A确认真实退出前Released仍未完成。
- GUI同编辑器双请求都批准：真实RED抛 `Sequence contains no matching element`、exit 1。批准后视觉移除按ReferenceEquals查找，已移除时幂等返回原子关闭结果，不再次清理或影响其他editor/detailHost；真实verifier覆盖两个待批准请求同时结束。

修复后定向CloseConfirmation **13/13**、完整V2 **282/282**，均exit 0；V2和样例Release构建均0警告、0错误；真实 `--verify-v2-close` **PASS**、exit 0，最终结果文件更新；diff --check通过且C#保持CRLF/UTF-8 BOM。原239样例和解决方案基线证据保留，不重复全范围。

日志 `14-review-red-remove.log`、`14-review-red-winner.log`、`14-review-red-ui.log`/`14-review-red-ui-result.txt`、`14-review-build.log`、`14-review-tests.log`、`14-review-full-v2.log`、`14-review-sample-build.log`、`14-review-close-run.log`。Root负责三处窄增量复核。

## Spec coverage

- 用户故事：US24、US33、US34、US35、US37、US38。
- 实现决策：D16、D22、D23、D24。
- 行为验收：T13、T14、T15、T17。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-02，独立审查的当前确认子退出重验、并发提交后取消返回原票据及重复批准视觉移除三项 P2 均已公开 RED/GREEN 修复；窄增量复核无遗留实质问题。整树最后全模型锁验证与提交、确认活动真实退出和 Scope 屏障有效。

最新定向 13/13、完整 v2 282/282、原有与样例 239/239（有效两组合计 521/521）全部通过；真实 `--verify-v2-close` 包括重复批准移除 PASS。窄修复保留既有完整构建与样例证据，最终样例/v2 增量构建 0 警告、0 错误及差异/格式检查通过。本票完成，导航跨请求依赖进入 15。
