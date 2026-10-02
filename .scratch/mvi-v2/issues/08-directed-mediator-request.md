# 08: 独立 Feature 通过 Mediator 定向请求

**What to build:** 查询与详情两个独立 Feature 通过业务契约完成请求响应；同类型详情多开时由宿主明确目标，目标更新自己的状态。

**Blocked by:** 03 — 异步 Operation 的验证、反馈与完成契约。

**Status:** completed

## Acceptance criteria

- [x] 发送者只依赖业务契约和目标地址或端口，接收方消息映射为 Intent 并走已有验证、操作与状态提交入口。
- [x] 范围内唯一提供方可自动绑定；多候选、缺失和不可用目标返回明确结果，不依靠类型名、排序或第一个候选。
- [x] 同类型多实例和同一业务对象多开均保持 State、操作和路由隔离；跨范围由宿主显式接线。
- [x] SendAsync 正常返回表示目标业务处理与有关状态提交完成；请求结果需要改变发送者状态时也通过其自己的反馈入口。
- [x] 无订阅或反订阅代码即可运行公开消费者演示；测试覆盖所有目标路径、无效业务输入和目标故障，不提供 Publish、Subscribe 或状态观察图。

## Spec coverage

- 用户故事：US09、US21、US22、US23、US25、US26、US27、US29、US30、US50。
- 实现决策：D01、D07、D08、D16、D17、D18、D19、D20。
- 行为验收：T02、T05、T08、T11。

这些编号引用 MVI v2 实施规格；这里表示本任务负责或贡献的覆盖，完整验收仍以该规格为准。

## 实现与验收记录

2026-10-02。实现与独立复核基线为任务 03 提交 `0d70b53507a543b9a063c71c1b204f7943de5f8c`。共享工作区同时存在其他任务的并发实现，08 在独立受管工作树实现、验证和审查，随后只集成本任务改动到 `codex/mvi-v2`。

公开测试边界沿用实施规格：Feature 调用、Mediator 契约端口、结构化结果和公开 Snapshot。首个公开消费者测试先因缺少 `RequestPort<TRequest, TResult>` 编译失败，再补齐实现。

- `Feature.CreateRequestPort<TRequest, TResult>` 将不可变请求捕获为目标 Operation 输入；验证依据当前 State 和本次请求，执行、反馈与终态提交复用既有 `DispatchOperation`。没有“先写请求字段，再启动操作”的双入口竞争。
- `Mediator.Register(port)` 由宿主建立显式契约范围；自动请求要求恰好一个已注册提供方，明确端口请求也必须在该范围接线。多目标流程由宿主逐个或并行请求指定端口，跨范围需宿主分别接线。
- `RequestResult<TResult>` 区分路由与等待结论；`Responded` 保留目标原始 `OperationResult<TResult>`，完成、验证拒绝、协作取消、故障和业务失败值分别表达。
- 路由回执只解除当前范围的后续目标选择，已选定请求不因解除路由而撤销；`RequestPort.Deactivate()` 永久停止该端口的新接纳，已接纳请求不因端口撤销而终止。目标执行仍等待自身反馈和已登记子工作完成。
- 请求等待令牌在接纳前阻止启动，接纳后只结束调用者等待；目标执行使用自己的 Operation 归属。发送者需要更新状态时，通过发送者自身的 `Operation.UpdateAsync` 反馈。

本票采用运行时显式接线和手写强类型接收适配；静态声明诊断、显式传播协作取消、Feature 逻辑关闭与 DI 资源释放、有界 Post 分别属于 16、09、10、11。端口撤销只定义通信接纳，不宣称实例或服务范围已经关闭。

## 验证

最终隔离工作树的 Release 构建通过：15 个项目，0 错误、0 警告；此前首次完整重编译出现的 5 条 MVI0013 来自原有 v1 测试夹具。最终完整测试 **337/337** 通过（原有测试 216/216，V2 121/121），失败和跳过均为 0。

新增请求公开行为测试 **19/19** 通过，覆盖所有路由结果、重复接线与移除、跨范围显式接线、同类型和同业务对象多开、运行身份隔离、程序与请求共用验证、验证及服务故障、业务失败值、原子启动采样、嵌套子工作完成、发送者自身反馈、多目标协调、等待取消和同步展示回调交叉请求。投影整合回归 8/8 通过。时序通过 TCS 与同步屏障控制，无任意延时或轮询。

独立无界面消费者 Release 构建及执行通过：state loop、operation loop、mediator loop 三组 PASS。查询 Feature 只依赖请求契约、Mediator 和宿主提供的端口；详情 Feature 独立维护状态，宿主负责同类型多开与跨范围接线。五个本票修改的 C# 文件均已确认 UTF-8 BOM、CRLF，无单独 LF。

实际执行的完整验证命令：

```powershell
rtk dotnet build MiKiNuo.Mvi.slnx -c Release --no-restore -p:UseSharedCompilation=false
rtk proxy dotnet test --solution MiKiNuo.Mvi.slnx -c Release --no-build --results-directory TestResults
rtk proxy test/MiKiNuo.Mvi.Headless.Consumer/bin/Release/net10.0/MiKiNuo.Mvi.Headless.Consumer.exe
```

公开 API 接线与结果语义见 `test/MiKiNuo.Mvi.Headless.Consumer/README.md`，运行演示见同目录 `MediatorDemo.cs`。

## Standards

最终独立复核：**0 项遗留问题**。复用既有 Operation 入口，端口与注册回执分别承担接纳控制和路由范围职责；消费者只通过自身操作反馈更新状态。完整七文件审查与后续两文件修复增量复核均通过，编译器已覆盖的风格和格式检查未重复审查。

## Spec

最终独立复核：**0 项遗留问题**。初审发现端口接纳锁包围 `FeatureStore.Start` 的同步展示回调，两个端口交叉请求会形成等待环。公开投影回归先 RED 复现，再将端口锁缩小到取消、可用性和接纳决定，释放锁后启动目标处理；修复后回归 GREEN，并以验证期间停用端口的场景确认已接纳请求继续完成。端口与范围锁均不包围目标展示回调，五项验收已满足。
