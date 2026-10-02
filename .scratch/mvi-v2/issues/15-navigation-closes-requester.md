# 15: 导航关闭发起者时解除等待环

**What to build:** 编辑器通过 Mediator 请求保存后导航，导航目标可以确认并关闭发起者的组合，再返回响应；调用和资源释放不会互相等待。

**Blocked by:** 09 — 取消请求等待与目标执行分别生效；14 — 整个组合确认关闭并抵抗状态变化。

**Status:** completed

## Acceptance criteria

- [x] 发起者等待目标请求期间，目标可以完成其所有权范围的关闭确认并逻辑关闭发起者。
- [x] 导航响应不等待发起者物理退出；响应返回后发起者才能结束的场景可稳定完成。
- [x] 调用方取消等待与目标执行归属保持既有语义；导航已完成的外部效果不因发起者关闭而撤销。
- [x] 发起者及其子实例资源在实际使用者退出后释放；可判定的自等待或依赖等待给明确诊断或结果。
- [x] 工作区演示与可控时序测试覆盖正常导航、关闭拒绝、确认期间变动、取消等待、目标故障与不合作子工作。

## Implementation and acceptance evidence

2026-10-02 实现与验证，基线 `b6b7cea`；新的执行上下文/请求依赖协议由Root独立审查，不进入切片16。

- 内部 `ExecutionFrame(owner, ValidateActive)` 区分真实Operation，消息发送前复用Store的Accepting/lifecycle/generation/token/failure校验。Send/Post/TryPost拒绝失效源，Latest身份失效不等取消回调；Parallel成员各自保持精确身份。
- 已接纳Send在Admit/Start前建立可继承活动 `RequestEdge(caller,target,parent)`，回应/WaitCanceled/错误finally停用；等待取消不取消TargetOwned。目标建立自身帧，Released除自身/tree/constructor保护外检查活动caller/ancestor，覆盖多层可判定请求等待环，没有全局任意Task图。
- Post不等待处理，不继承sender等待链；消费者与调度入口fresh。Projection.RequestDisplay/Display及操作展示/诊断回调临时清业务帧与请求链，finally恢复，原真实同步构造保护保留；同步/排队展示输入和消息是新入口。
- 真实保存child只发送不可变Requester实例ID与Destination契约。宿主生命周期适配器按ID请求Root.RequestCloseAsync，独立导航Feature逻辑关闭后立即返回并提交自己的路由结果，不等待Requester Released。
- 保存发起者默认独立等待，不误把自身生命周期token传成等待取消；拿到导航响应后业务/未合作Track才真实退出，scope及原父票据随最后使用者释放。已提交保存与导航事实不回滚。
- TCS公开矩阵覆盖导航关闭响应先于Scoped资源退出、直接caller祖先Released拒绝、多层链等待和WaitCanceled后边失效/目标继续、导航veto/targetFault、Latest Send/Post/TryPost阻断、Parallel身份隔离、同步及排队展示fresh输入/消息。确认期间条件变化由14完整回归保持。

实际验证：

1. V2测试项目与Avalonia样例Release构建 `--no-restore -p:UseSharedCompilation=false` — 均exit 0，0警告、0错误。
2. V2可执行程序 `--maximum-parallel-tests 1 --treenode-filter "/*/*/NavigationRequestTests/*"` — **10/10**，exit 0。
3. V2完整运行 `--maximum-parallel-tests 1` — **292/292**，0失败、0跳过，exit 0。
4. `rtk proxy sample/MiKiNuo.Mvi.Samples.Avalonia/bin/Release/net10.0/MiKiNuo.Mvi.Samples.Avalonia.exe --verify-v2-navigation --v2-navigation-result=F:/MiKiNuoProjects/MVI/.scratch/mvi-v2/15-navigation-result.txt` — **PASS v2-navigation**，真实HWND/UI线程、不可变ID契约、组合逻辑关闭先响应、scope/Track及父Released先后、业务事实保留均通过，自动退出exit 0。
5. 解决方案Release构建 — exit 0，0错误、5条既有v1 MVI0013警告。
6. 原有/认证样例测试 `--maximum-parallel-tests 1` — **239/239**，0失败、0跳过，exit 0。
7. Headless.Consumer state/operation/queue/mediator/post五项自检全部PASS，exit 0。
8. `rtk proxy git diff --check` — exit 0，修改及新增C#保持CRLF/UTF-8 BOM。

结果文件 `.scratch/mvi-v2/15-navigation-result.txt`；日志 `15-build.log`、`15-sample-build.log`、`15-tests.log`、`15-full-v2.log`、`15-navigation.log`、`15-solution-build.log`、`15-original-tests.log`、`15-headless.log`。Root负责独立审查与提交。

## Independent review context corrections

2026-10-02 一P1与两P2公开RED→GREEN：

- fresh同步展示RED：源执行结束通知中读取自身Released未被拒绝，exit 2；真的同步等待会形成资源死环。fresh业务帧仍清空，但ThreadStatic同步scope保留原resource owner和连续活动请求链；Released检查同步栈依赖，fresh回调同步Send也使用实际资源caller建立等待边。该scope不流到独立异步UI线程，构造守卫保留。两个结束栈测试使用TCS保证真实Finish回调，不用快服务合并快照假设。
- 确认帧RED：ConfirmCloseAsync内Send/Post/TryPost三变体因没有用户OperationStates被误认Superseded，结果Rejected，exit 2。确认依据activeExecutions/Accepting/lifecycle/token/failure检查，只有普通业务执行检查RunningIds/generation。
- 中段链RED：A→B→C中B停止等待C后，C误继承A→B依赖，exit 2。只遍历从当前目标开始连续active链，遇失效边截断；全活动多层链的原拒环测试继续通过。
- 回归额外证明同步fresh新Send可发且target识别真实source资源依赖，以及独立异步显示工作可以合法等待source真正释放。

最终增量：导航定向 **17/17**、完整V2 **299/299**，均exit 0；V2/样例Release构建均0警告、0错误；真实 `--verify-v2-navigation` **PASS**、exit 0，最终结果文件更新；diff --check通过，C#保持CRLF/UTF-8 BOM。原239样例/Headless/解决方案证据保留。

日志 `15-review-red-fresh.log`、`15-review-red-confirmation.log`、`15-review-red-chain.log`、`15-review-build.log`、`15-review-tests.log`、`15-review-full-v2.log`、`15-review-sample-build.log`、`15-review-navigation.log`。Root负责上述三处增量复核。

最后窄寿命绑定：fresh来源RequestEdge同时依赖创建时同步FreshScope的active标志，scope返回即物理依赖结束；真正Operation来源边继续由Send等待finally控制。新增普通async PropertyChanged事件同步前半段Send（非Task.Run隔离），目标TCS等待source已退出后读取Released合法；原同步blocking fresh Send和直接getter仍拒绝环。

- 最新包含该回归的工厂构建exit 0，0警告、0错误；定向导航 **18/18**，完整V2 **300/300**，均exit 0。
- 日志 `15-scope-edge-build.log`、`15-scope-edge-tests.log`、`15-scope-edge-full-v2.log`；此前真实navigation PASS及原样例证据保留。

## Spec coverage

- 用户故事：US13、US27、US33、US34、US35、US36、US37、US39、US40。
- 实现决策：D12、D15、D19、D20、D22、D23、D24。
- 行为验收：T10、T13、T14、T16、T17。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## Root 最终验收

2026-10-02，独立审查的同步展示释放自等待、确认活动发送校验及中段断开请求链均已公开 RED/GREEN 修复，增量复核确认原问题解决。最后 fresh 同步 scope 寿命回归将资源边绑定原同步 scope，回调返回即解除物理依赖；Root 已核对共享 active 标志与普通 async 事件公开回归，真正 Operation Send 仍按等待期管理。

最新定向 18/18、完整 v2 300/300 通过；原有与样例 239/239、Headless 五组与真实 Windows 导航 PASS 证据有效保留。窄修复后的编译 0 警告、0 错误及差异/格式检查通过。导航响应与逻辑关闭不等待请求者资源释放，晚退出工作保持归属；本票无遗留重要问题。
