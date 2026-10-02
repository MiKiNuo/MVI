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

`OperationStates[name].RunningIds` 是按接纳顺序排列、仍在真实执行或等待所属工作退出的不可变身份集合；`RunningCount` 与 `IsRunning` 从该集合派生。兼容属性 `RunningId` 返回最早接纳且尚未退出的身份，全部退出后为空。`LastAttemptId`、`LastResult`、`Reason`、`Exception` 表达最近发布的调用反馈，按反馈提交顺序演进。拒绝和预取消只改变调用反馈并保留其他身份；执行结束仅移除自身身份，仍有其他执行时保持 Busy。每次执行使用自己的开始输入、取消令牌与强类型结果，`UpdateAsync` 基于提交时的当前 State 应用纯规则；业务冲突条件由规则或外部服务表达。执行结果区分 Completed、Rejected、Canceled、Superseded、Faulted，正常业务失败值仍是 Completed。当前支持 Reject 与有界 Parallel，Superseded 为后续策略预留。取消不回滚既有提交，晚反馈不能正常成功；纯规则故障保留原业务状态并产生带异常的 Faulted。

业务方法应等待其子工作，或在退出前用 `operation.Track(task)` 登记仍可能访问实例资源的已启动工作。完成屏障继续接纳这些子工作登记的嵌套工作，取消或故障期间仍等待真实退出；屏障结束后的登记和更新会失败。Completed 保证业务方法、已登记工作和有关状态提交完成。示例通过可控服务完成信号自检启动验证、慢服务期间编辑、强类型反馈与完成，无需 UI 或额外依赖。
