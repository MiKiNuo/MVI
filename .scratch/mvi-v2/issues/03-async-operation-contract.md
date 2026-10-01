# 03: 异步 Operation 的验证、反馈与完成契约

**What to build:** 无界面消费者可以调用一个带启动验证的异步业务操作，继续提交其他输入，并得到与已提交状态一致的强类型执行结果。

**Blocked by:** 01 — 独立核心消费者跑通纯状态闭环。

**Status:** completed

## Acceptance criteria

- [x] 生成的公开调用进入统一启动入口，启动验证、默认重复拒绝与输入快照采样原子一致；拒绝不产生执行 Effect、不调用服务。
- [x] Effect 在提交门外运行；等待服务期间其他合法输入仍可处理。Operation 开始快照与 UpdateAsync 基于当前 State 的转换分开，更新通过内部 Intent 回流。
- [x] 业务 State 与运行状态同快照演进；可见拒绝反馈来自快照；重复尝试拒绝不能清除当前运行状态。
- [x] 正常等待完成保证业务方法结束与已接纳状态提交；业务失败值、拒绝、协作取消和故障可区分，取消不回滚既有提交，失效更新不伪装成功。
- [x] 用可控服务完成信号覆盖验证竞争、并发编辑保留、服务失败、取消、纯规则故障及操作子工作跟踪，不依靠任意延时制造时序。

## Spec coverage

- 用户故事：US09、US10、US11、US12、US13、US14、US15、US16、US20。
- 实现决策：D06、D07、D08、D09、D10、D11、D12、D13、D14、D15。
- 行为验收：T02、T03、T04、T05、T06。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## 实现与验收记录

2026-10-02，合并到 `codex/mvi-v2`。最终审查基线为任务 02 提交 `6eeea18fb8f4eb85f47e091c7005d977ee4219c2`；03 的独立实现先在受管工作树验证，再保留 02 的输入、投影与真实平台绑定完成三方合并。

- `[Operation(Validate = nameof(CanSubmit))]` 支持 private `ValueTask<TResult>` 或 `Task<TResult>` 方法，接收 `Operation<TState>`；生成 public `Task<OperationResult<TResult>> SubmitAsync(CancellationToken cancellationToken = default)`。
- `Operation<TState>.Snapshot` 是启动时的一致输入；`UpdateAsync(reducer, payload)` 将强类型反馈 Intent 交给当前 State 的纯转换；`Track(Task)` 将子工作和嵌套工作纳入真实退出屏障。
- `RuntimeSnapshot.OperationStates` 保存不可变运行身份和最近反馈。重复拒绝保留当前 `RunningId`，最终结果明确区分执行完成、拒绝、协作取消、被取代与故障，业务失败值仍可正常完成。
- 启动、反馈与完成都经过内部 Intent、纯 Reduce 和唯一 Store 提交点。未接纳的验证故障精确保留原快照；已接纳操作的故障或取消不回滚既有提交。
- 与 02 的本地投影共用提交队列：门内入队、门外调度，正常完成不等待绘制。投影与宿主诊断回调错误独立处理，诊断不输出状态、业务载荷、异常消息或调用栈。

本票只交付默认重复拒绝及上述执行协议。Latest、Queue、Parallel、Mediator、实例关闭和平台认证命令继续由对应后续任务负责；`Superseded` 保留为公共结果种类。

## 验证

最终 Release 解决方案构建成功：15 个项目，0 错误、0 警告。此前完整重编译出现的 5 条 MVI0013 来自原有 v1 测试夹具，未为本票修改这些夹具。

最终完整测试 **318/318** 通过（原有测试 216/216，V2 102/102），失败和跳过均为 0；独立无界面消费者的 state loop 与 operation loop 均 PASS。Operation 运行回归 23 项、声明与消费者编译回归 17 项、投影/诊断整合回归 8 项；竞争顺序使用 TCS 或同步信号，超时仅作为挂起保护，不使用任意延时制造时序。

实际执行命令：

```powershell
rtk dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false
rtk proxy dotnet test --solution MiKiNuo.Mvi.slnx -c Release --no-build --results-directory TestResults
rtk proxy test/MiKiNuo.Mvi.Headless.Consumer/bin/Release/net10.0/MiKiNuo.Mvi.Headless.Consumer.exe
```

无界面示例及公开 API 用法见 `test/MiKiNuo.Mvi.Headless.Consumer/README.md`。

## Standards

最终独立复核：**0 项遗留问题**。启动/完成绕过 Intent–Reduce 的初审问题已改为统一纯转换和提交；诊断监听器异常也已隔离。两个合并源文件保留了 02 与 03 的职责，编译器已覆盖的风格和格式检查未重复审查。

## Spec

最终独立复核：**0 项遗留问题**。linked-token 协作取消分类、验证异常精确保留快照、投影调度错误破坏可等待结果的问题均先复现再修正；启动、反馈、完成及诊断故障回归均通过。没有遗留的重要实现或整合问题。
