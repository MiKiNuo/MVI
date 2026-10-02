# 无界面状态与操作消费者

运行 `dotnet run --project test/MiKiNuo.Mvi.Headless.Consumer -c Release`。消费者仅引用核心运行时；生成器引用使用 `OutputItemType="Analyzer"` 与 `ReferenceOutputAssembly="false"`。

作者声明不可变 State 和直接继承 `Feature<TState>` 的顶层、非泛型 partial 类。`[Input]` 仅用于 public 实例属性的 public get/init；框架生成 `Set<Property>(value)`。未标记属性没有输入入口。`[OnInput(nameof(State.Property))]` 规则必须是精确的 `private static State Method(State state, PropertyType value)`，只替换这个属性的默认 with 回写；规则必须保持纯转换。

当前支持在消费者源码中声明的密封 record 类、readonly record struct 及递归不可变成员，包括基础值类型、字符串、枚举、Guid、日期/时间、Nullable，以及元素满足相同约束的 ImmutableArray、ImmutableList、ImmutableQueue、ImmutableStack。继承成员也接受验证。其他程序集中的未知记录及其继承链会被拒绝，因为默认元数据导入不能证明其私有存储不可变；明确支持的 BCL 标量与不可变集合不受此限制。普通类、未密封记录、可写 setter、显式实例字段、数组、可变集合、只读集合包装或接口、其他尚未验证的类型会被声明诊断拒绝；不能仅凭 record 或 IReadOnlyList 认定不可变。

Snapshot 同时提供 State、Version 和不可变 OperationStates。成功输入在实例短提交门中依据当前 State 计算并提交单调版本，并保留运行中的操作元数据；规则抛异常、返回 null 或重入同一实例时不提交业务状态。输入允许空字符串等中间值。

`[Operation(Validate = nameof(CanSubmit))]` 标记 `private ValueTask<TResult> Method(Operation<State> operation)`，也支持 `Task<TResult>`。生成同名 `public Task<OperationResult<TResult>> Method(CancellationToken cancellationToken = default)`；验证必须是 `private static bool CanSubmit(State state)`。启动验证、同名重复拒绝与开始输入采样在同一短提交门中完成，后台服务运行时仍可编辑。`operation.Snapshot` 是开始输入，`await operation.UpdateAsync(reducer, payload)` 通过内部强类型反馈输入依据提交时的当前 State 转换，保留其他并发编辑。

`OperationStates[name].RunningId` 表达仍在真实执行的操作；`QueuedCount` 表达已接纳但尚未启动的等待项数。`LastAttemptId`、`LastResult`、`Reason`、`Exception` 表达最近发布的调用反馈，按反馈提交顺序演进。重复拒绝只改变调用反馈并保留 RunningId；当前执行随后结束时，完成反馈替换先前拒绝反馈并清除自己的 RunningId。执行结果区分 Completed、Rejected、Canceled、Superseded、Faulted，正常业务失败值仍是 Completed。取消不回滚既有提交，晚反馈不能正常成功；纯规则故障保留原业务状态并产生带异常的 Faulted。Superseded 为后续策略预留。

顺序作业显式声明 `[Operation(Concurrency = OperationConcurrency.Queue, Capacity = 1, Validate = nameof(CanSubmit))]`；默认策略仍为 Reject。Capacity 必须为正数，只计算等待名额，另有最多一个运行项；非法配置产生 `MVI2010`。满载返回 `Rejected` 和 `QueueFull`，不调用服务。每个调用返回的 Task 等待自己的真实结果，快照可区分运行和排队。等待项按接纳顺序启动，实际启动时原子校验当前 State 并采样 `operation.Snapshot`。排队取消及时返回 Canceled 并释放名额，已取消项不再启动；运行项取消后仍等服务及已登记工作退出，再启动下一项。业务失败或故障不会丢弃后续已接纳项。

业务方法应等待其子工作，或在退出前用 `operation.Track(task)` 登记仍可能访问实例资源的已启动工作。完成屏障继续接纳这些子工作登记的嵌套工作，取消或故障期间仍等待真实退出；屏障结束后的登记和更新会失败。Completed 保证业务方法、已登记工作和有关状态提交完成。示例通过可控服务完成信号自检启动验证、慢服务期间编辑、强类型反馈与完成，以及有界 Queue 的顺序、满载、启动输入和逐项结果，无需 UI 或额外依赖。
