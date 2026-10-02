# 11: 定向 Post 明确接纳与后续失败

**What to build:** 发送者可以向确定实例投递无需等待业务结果的消息，立即区分接纳、满载或关闭拒绝，并追踪接纳后的处理失败。

**Blocked by:** 10 — 单实例逻辑关闭与服务范围安全释放。

**Status:** completed

## Acceptance criteria

- [x] Post 与 TryPost 面向确定目标实例，复用实例入口和运行协议，不为每种消息或接收者建立单独队列。
- [x] 接纳报告仅证明消息进入目标处理边界；它不表示验证通过、业务处理结束或状态提交完成。
- [x] 接纳有界且拒绝原因可识别；满载或已关闭时不静默丢弃，也不返回虚假的成功。
- [x] 接纳后按处理时的状态验证，后续拒绝或故障通过关联的结果记录或诊断表达；不引入用于这些报告的观察者订阅通道。
- [x] 可控消费者证明先接纳后处理、容量拒绝、业务验证拒绝、故障关联和明确多目标分派；使用真实逻辑关闭入口证明关闭及在途释放期间不能再接纳新 Post。

## Implementation and acceptance evidence

2026-10-02 实现与测试完成，依据 ADR 0025、D19/D20；新并发接纳协议的独立审查由 Root 执行。

- `Mediator.Post(message, target)` 仅接受宿主明确选定并已接线的现有 `RequestPort<TMessage,TResult>`。`TryPost(..., out receipt)` 返回是否接纳，同时保留完整 `PostReceipt<TResult>`；没有新端口体系、发布订阅或观察者通道。
- 回执包含唯一 `Id`、`Accepted / InboxFull / TargetUnavailable` 和已接纳消息的 `Completion`。拒绝接纳没有完成任务；接纳不承诺验证通过或业务结束，后续原始 `OperationResult.OperationId` 与回执 Id 一致。
- 每个 `FeatureStore` 只有一个异构 FIFO。`Feature(initialState, postCapacity: 16)` 的正数容量按该实例全部未终结 Post 计算，包含正在处理项；所有契约与端口共享，完成后归还容量。
- Store 门内只决定关闭、容量、入队和消费者归属；门外单消费者逐项进入既有 `Start`，根据处理时的当前状态执行原子验证、输入采样、并发准入及反馈。同名操作继续与 Send/程序调用共用 Reject/Queue 等策略。
- 入箱到调度、出箱到 Start 的间隔由消费者归属覆盖。逻辑关闭阻止入箱并移除等待消息，给原回执 `Canceled / Closed`；处理中业务、嵌套 Track、CTS 清理与消费者退出前不允许 Scope Released。
- 路由解绑先于目标选择时返回明确不可用，已选定路由仍须通过端口与 Store 的接纳决定。端口停用与 Store 纯入箱决定在端口门内原子排序，仅真正已入箱消息继续；消费者调度在端口门外。锁顺序仅 port→Store，Close/RegisterPort 的停用在 Store 门外，不存在 Store→port 持锁。所有验证、业务、投影和回执续体均不在路由/端口接纳锁内执行。
- `MediatorPostTests` 用 TCS/显式屏障覆盖接纳与完成分离、跨类型容量、16项并发容量竞争、处理时验证拒绝/故障、服务故障后 FIFO 推进、关联身份、两目标与实例隔离、路由解绑/停用、Direct/Send 并发入口、Operation Queue 共用、真实关闭、未合作 IO/Track 资源保留、20轮接纳/调度/关闭竞争。内联投影在当前操作完成时确定地关闭，下一条已接纳消息不启动。
- Headless.Consumer 默认路径增加 Post 自检，覆盖真实消费者程序集中的接纳、满载、处理时验证、关联完成与关闭。

实际验证：

1. `rtk proxy dotnet build test/MiKiNuo.Mvi.V2.Tests/MiKiNuo.Mvi.V2.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false` — exit 0，0 警告、0 错误。
2. `rtk proxy test/MiKiNuo.Mvi.V2.Tests/bin/Release/net10.0/MiKiNuo.Mvi.V2.Tests.exe --maximum-parallel-tests 1 --treenode-filter "/*/*/MediatorPostTests/*"` — **12/12**，0 失败、0 跳过，exit 0。
3. `rtk proxy test/MiKiNuo.Mvi.V2.Tests/bin/Release/net10.0/MiKiNuo.Mvi.V2.Tests.exe --maximum-parallel-tests 1` — **251/251**，0 失败、0 跳过，exit 0。
4. `rtk proxy dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false -v:minimal` — exit 0，0 错误，5 条与基线一致的 v1 `MVI0013` 警告。
5. `rtk proxy test/MiKiNuo.Mvi.Headless.Consumer/bin/Release/net10.0/MiKiNuo.Mvi.Headless.Consumer.exe` — state、operation、queue、mediator、post 五项自检全部 PASS，exit 0。
6. `rtk proxy git diff --check` — exit 0；修改与新增的 C# 文件保持 CRLF 和 UTF-8 BOM。

完整证据日志：`.scratch/mvi-v2/11-build.log`、`11-tests.log`、`11-full-v2-tests.log`、`11-solution-build.log`、`11-headless.log`。Scope 不合作执行无退出时间上界，接纳回执的完成等待取消不撤销目标已经接纳的工作。

## Port admission ordering correction

2026-10-02 核对原 `Deactivate` 契约后，纯入箱决定移至端口门内，内部返回回执与可选消费者调度动作；端口门外才调度，不执行验证或平台回调。公开 API 不变。

- 新公开测试以纯输入转换屏障占住 Store 门，验证 Post/Deactivate 竞争的明确排序，并验证已停用端口无须等待 Store 门便拒绝投递；没有暴露私有 gate 或收件箱。
- 定向测试 `--treenode-filter "/*/*/MediatorPostTests/*" --maximum-parallel-tests 1` — **14/14**，exit 0。
- 完整 V2 `--maximum-parallel-tests 1` — **253/253**，0 失败、0 跳过，exit 0。
- V2 测试项目及 Headless.Consumer Release 构建，`--no-restore -p:UseSharedCompilation=false` — 均 exit 0，0 警告、0 错误。
- Headless.Consumer 默认五项自检全部 PASS，exit 0。
- 原解决方案构建与251项基线证据保留；上述增量结果为端口排序修正后的最终验收。

新增日志：`.scratch/mvi-v2/11-order-build.log`、`11-order-tests.log`、`11-order-full-v2.log`、`11-order-consumer-build.log`、`11-order-headless.log`。Root 负责该窄修正的增量复核与提交。

## Spec coverage

- 用户故事：US14、US26、US28、US30、US50。
- 实现决策：D15、D17、D18、D19、D20。
- 行为验收：T09、T11、T17。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-02，独立并发审查确认容量、回执关联、故障推进及真实退出归属没有遗留重要问题。端口停用与实际入箱的排序按原契约收紧，并以公开交错回归验证；增量复核确认仅 port → Store 锁顺序，所有消费者调度在端口锁外。最终 Post 14/14、完整 v2 253/253，Headless 五组自检全部 PASS；所有实际命令退出码 0，最终差异及格式检查通过。
