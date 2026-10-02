# 无界面状态与操作消费者

从仓库根目录运行默认状态与操作闭环，或运行有界并行演示：

```powershell
rtk proxy dotnet run --project test/MiKiNuo.Mvi.Headless.Consumer -c Release
rtk proxy dotnet run --project test/MiKiNuo.Mvi.Headless.Consumer -c Release -- --parallel
```

消费者仅引用核心运行时；生成器引用使用 `OutputItemType="Analyzer"` 与 `ReferenceOutputAssembly="false"`。并行演示启动两个独立请求，第三次调用明确拒绝；第二项先完成时仍显示一项运行，最终反馈累计为 10，保留 IO 期间的新编辑与旧不可变快照。每条 PASS 都由可运行自检断言确认。

作者声明不可变 State 和直接继承 `Feature<TState>` 的顶层、非泛型 partial 类。`[Input]` 仅用于 public 实例属性的 public get/init；框架生成 `Set<Property>(value)`。未标记属性没有输入入口。`[OnInput(nameof(State.Property))]` 规则必须是精确的 `private static State Method(State state, PropertyType value)`，只替换这个属性的默认 with 回写；规则必须保持纯转换。

当前支持在消费者源码中声明的密封 record 类、readonly record struct 及递归不可变成员，包括基础值类型、字符串、枚举、Guid、日期/时间、Nullable，以及元素满足相同约束的 ImmutableArray、ImmutableList、ImmutableQueue、ImmutableStack。继承成员也接受验证。其他程序集中的未知记录及其继承链会被拒绝，因为默认元数据导入不能证明其私有存储不可变；明确支持的 BCL 标量与不可变集合不受此限制。普通类、未密封记录、可写 setter、显式实例字段、数组、可变集合、只读集合包装或接口、其他尚未验证的类型会被声明诊断拒绝；不能仅凭 record 或 IReadOnlyList 认定不可变。

Snapshot 同时提供 State、Version 和不可变 OperationStates。成功输入在实例短提交门中依据当前 State 计算并提交单调版本，并保留运行中的操作元数据；规则抛异常、返回 null 或重入同一实例时不提交业务状态。输入允许空字符串等中间值。

`[Operation(Validate = nameof(CanSubmit))]` 标记 `private ValueTask<TResult> Method(Operation<State> operation)`，也支持 `Task<TResult>`。生成同名 `public Task<OperationResult<TResult>> Method(CancellationToken cancellationToken = default)`；验证必须是 `private static bool CanSubmit(State state)`。启动验证、同名重复拒绝与开始输入采样在同一短提交门中完成，后台服务运行时仍可编辑。`operation.Snapshot` 是开始输入，`await operation.UpdateAsync(reducer, payload)` 通过内部强类型反馈输入依据提交时的当前 State 转换，保留其他并发编辑。

同名重复启动默认得到 Rejected/AlreadyRunning。独立并行业务显式声明 `[Operation(Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]`；Parallel 必须声明正数上限，Reject 使用零表示未配置并行上限。未知策略、非正并行上限或 Reject 配置非零上限产生 MVI2010 声明诊断；直接使用受保护启动入口时得到 Rejected/InvalidConcurrency。并行容量不足得到 Rejected/ConcurrencyLimitReached，并不启动业务方法。

`OperationStates[name].RunningIds` 是按接纳顺序排列、仍在真实执行或等待所属工作退出的不可变身份集合；`RunningCount` 与 `IsRunning` 从该集合派生；`QueuedCount` 表达已接纳但尚未启动的 Queue 等待项数。兼容属性 `RunningId` 返回最早接纳且尚未退出的身份，全部退出后为空。`LastAttemptId`、`LastResult`、`Reason`、`Exception` 表达最近发布的调用反馈，按反馈提交顺序演进。拒绝和预取消只改变调用反馈并保留其他身份；执行结束仅移除自身身份，仍有其他执行时保持 Busy。每次执行使用自己的开始输入、取消令牌与强类型结果，`UpdateAsync` 基于提交时的当前 State 应用纯规则；业务冲突条件由规则或外部服务表达。执行结果区分 Completed、Rejected、Canceled、Superseded、Faulted，正常业务失败值仍是 Completed。当前支持 Reject、Latest、有界 Queue 和 Parallel；Latest 请求取消并取代旧反馈身份。取消不回滚既有提交，晚反馈不能正常成功；纯规则故障保留原业务状态并产生带异常的 Faulted。

顺序作业显式声明 `[Operation(Concurrency = OperationConcurrency.Queue, Capacity = 1, Validate = nameof(CanSubmit))]`；默认策略仍为 Reject。Capacity 必须为正数，只计算等待名额，另有最多一个运行项；非法配置产生 `MVI2010`。满载返回 `Rejected` 和 `QueueFull`，不调用服务。每个调用返回的 Task 等待自己的真实结果，快照可区分运行和排队。等待项按接纳顺序启动，实际启动时原子校验当前 State 并采样 `operation.Snapshot`。排队取消及时返回 Canceled 并释放名额，已取消项不再启动；运行项取消后仍等服务及已登记工作退出，再启动下一项。业务失败或故障不会丢弃后续已接纳项。

业务方法应等待其子工作，或在退出前用 `operation.Track(task)` 登记仍可能访问实例资源的已启动工作。完成屏障继续接纳这些子工作登记的嵌套工作，取消或故障期间仍等待真实退出；屏障结束后的登记和更新会失败。Completed 保证业务方法、已登记工作和有关状态提交完成。示例通过可控服务完成信号自检启动验证、慢服务期间编辑、强类型反馈与完成，以及有界 Queue 的顺序、满载、启动输入和逐项结果，无需 UI 或额外依赖。

## 定向查询与详情组合

`MediatorDemo.cs` 展示两个独立可复用的 `QueryFeature` 与 `DetailsFeature`。详情实例通过 `CreateRequestPort<TRequest, TResult>(operationName, validate, execute)` 将不可变请求映射到既有操作启动 Intent；当前状态与本次请求在同一提交区间验证，业务方法接收通过验证的 `Operation.Snapshot`，用 `Operation.UpdateAsync` 提交自己的状态。发送者仅持有业务契约、Mediator 和宿主选择的强类型端口，不依赖详情实现或状态类型。当前端口适配由作者显式声明，未扩展生成器。端口可通过可选 `concurrency`、`capacity`、`maxConcurrency` 参数复用同名生成操作的声明，省略时仍为 Reject。实例首次接纳同名执行后固定该并发配置；其他入口策略或界限不一致时返回 `Rejected/OperationConfigurationMismatch`，保留已有运行与等待项。

每个 `new Mediator()` 是独立通信范围。宿主调用 `Register(port)` 建立接线，返回的 `IDisposable` 只用于宿主清理该范围的路由，不是业务订阅。相同端口重复登记返回同一回执，不产生额外候选；回执释放后可重新接线，旧回执不会删除新路由。范围的候选集由已登记端口组成：唯一候选支持 `SendAsync<TRequest, TResult>(request)`；多候选返回 `AmbiguousTarget`，必须使用 `SendAsync(request, targetPort)`。请求与返回值类型共同确定契约，不使用类型名或注册顺序选目标。没有候选返回 `MissingTarget`；指定端口未在本范围接线或已经停用时返回 `TargetUnavailable`。跨范围需要宿主显式向另一个 Mediator 登记端口。

`port.Deactivate()` 永久阻止该端口后续接纳，与请求接纳原子排序；已接纳目标工作继续执行和提交反馈。停用不会自动移除范围候选，宿主需要释放注册回执更新组合接线。范围锁只保护候选选择，不包围目标验证或执行；解除注册阻止后续目标选择，已选定请求仍可进入尚可用的端口。这些句柄只管理请求接线和接纳，不承担 Feature 关闭或资源释放。

`RequestResult.Kind` 区分 `Responded`、缺失、歧义、不可用和 `WaitCanceled`；`Responded` 携带原始 `OperationResult<TResult>`，保留目标的拒绝、取消、故障与正常业务返回值。正常业务失败值仍可为 `Completed`。SendAsync 正常取得目标结果意味着业务方法、已登记工作和有关状态提交完成；响应需要改变查询状态时，查询通过自己的 `Operation.UpdateAsync` 更新。传入的 `waitCancellationToken` 在接纳前取消时不启动目标，接纳后只取消调用方等待，目标执行默认不继承它；显式传播取消策略属于后续任务。

演示自检唯一与歧义路由、同类型详情处理同一业务对象时的实例隔离、宿主明确多目标协调、目标验证、业务失败值、跨范围接线和不可用端口。整个组合没有订阅、反订阅、Publish、Subscribe 或跨实例状态观察图。
