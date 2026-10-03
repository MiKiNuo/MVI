# MiKiNuo.Mvi v2 运行架构

当前默认源码实现 v2，切片 20 退役已无调用者的 v1 运行时、生成容器、旧角色样板和测试约束。本文对应当前类型与协议；完整验收条件以 [SPEC.md](SPEC.md) 的 T01–T21 为准，进度与证据见 [任务看板](TICKETS.md)。性能目标由 21、22 的固定负载测量评价。

活跃术语见 [CONTEXT.md](CONTEXT.md)。[设计讨论](../mvi-next-design.md) 与 ADR 0009 onward 保留决策过程；跨 Feature 通信以 [ADR 0025](../adr/0025-vnext-mediator-without-observer-subscriptions.md) 为准，MVI 角色映射见 [ADR 0026](../adr/0026-vnext-explicit-mvi-role-mapping.md)。

## MVI 闭环与源码映射

![MVI 数据流](diagrams/mvi-dataflow.png)

`View = f(RuntimeSnapshot)`。输入进入实例的统一验证与纯转换入口，提交新快照后调度所属 View；副作用执行发生在提交门外，结果再次以 Intent 回到同一提交路径。业务作者以 State、Feature、View 声明这些角色，无需另写通用 Dispatcher 或 ViewModel。

| 角色 | 当前实现 | 责任 |
| --- | --- | --- |
| Feature | `Feature<TState>` 与业务 partial 子类型 | 生成输入/操作/请求入口；每实例独立 |
| Intent | `InputIntent<TState,TValue>`、`OperationStartIntent<TState,TResult>`、`FeedbackIntent<TState,TPayload>`、`OperationCompletedIntent<TResult>` 等内部值描述 | 携带输入、执行身份与反馈 |
| Store | `FeatureStore<TState>`，位于 Core 的 Feature.cs | 保存快照、短提交门、操作及有界 Post 入口 |
| Reducer | `FeatureStore` 的纯 `Reduce` 重载及业务纯方法 | 计算快照、启动决定和 Effect；不执行 IO |
| Effect | `OperationEffect<TState,TResult>` | 声明已接纳的执行及通过验证的输入 |
| Effect 执行 | `FeatureStore.ExecuteAsync` 与 `Operation<TState>` | 提交门外调用业务方法，反馈经 `UpdateAsync` 回流 |
| Reduction | `OperationReduction<TState,TResult>` | 统一表达 Snapshot、Result、Effect、Decision |
| View | 生成 `Projection` 继承 `FeatureProjection<TState>`，加平台适配 | 本地字段展示、命令与输入；不跨实例读 State |
| Mediator | `Mediator`、`RequestPort<TRequest,TResult>`、`FeatureMember.Wire` | 宿主明确目标；定向接纳、处理及相关结果 |
| 生命周期 | `FeatureOwnership`、`CloseTicket`、`FeatureFactory` | 所有权、关闭确认、真实退出及 Scope 释放 |

这些职责由已有 Core 类型和方法承担。旧设计名 FeatureDefinition、FeatureRuntime、GeneratedReducer、EffectRunner、ReductionResult 不是当前公开 API，也不存在额外运行对象层。

![当前类型 UML](diagrams/uml-types.png)

`Feature<TState>` 拥有一个 `FeatureStore<TState>`，快照包含 State、OperationStates 和 Version。纯输入 Reduce 直接返回快照，操作 Reduce 返回 `OperationReduction`。同实例同时只允许一个活动 View 投影，稳定挂载复用连接；卸载仅释放 View 自有的投影和原生信号，之后可重新挂载。

## 项目与包

![组件边界](diagrams/uml-components.png)

| 源项目 | 发布资产 |
| --- | --- |
| `src/MiKiNuo.Mvi` | `MiKiNuo.Mvi`：Core 运行时、输入与操作属性、组合、路由、投影、标准 DI 工厂 |
| `src/MiKiNuo.Mvi.Generators` | 仅 Core 包的 `analyzers/dotnet/cs/MiKiNuo.Mvi.Generators.dll` |
| `src/MiKiNuo.Mvi.Avalonia` | `MiKiNuo.Mvi.Avalonia`：本地绑定、Dispatcher、AvaloniaFeatureHost |
| `src/MiKiNuo.Mvi.Godot` | `MiKiNuo.Mvi.Godot`：原生信号、主线程投影队列、GodotFeatureHost |

平台包依赖 Core，NuGet 自动转递其唯一 analyzer；没有第四个框架运行包或额外 buildTransitive 适配。业务消费者可只用 Core，单平台明确安装平台入口，双平台同项目只加载一份框架生成器。外部宿主自行提供主题、桌面依赖或 Godot SDK/场景。

Avalonia 平台公开 namespace 仍为 `MiKiNuo.Mvi.Platforms.Avalonia`，程序集与包为新的 `MiKiNuo.Mvi.Avalonia`。这是已实现的 v2 类型，不代表旧平台项目仍保留。

## 业务声明与生成

State 使用不可变 record；`[Input]` 在可编辑字段声明一次，生成 `Set<Property>` 和投影回写，其他 State 属性只读。需要附加纯转换时声明 `[OnInput(nameof(State.Property))]`，普通输入不必另写规则。

业务 partial 子类型继承 `Feature<TState>` 即参与生成。`[Operation]` 方法接收 `Operation<TState>`，生成强类型可等待入口及投影的 `<MethodName>Command`。例如 private `SubmitAsync(Operation<LoginState>)` 对应 public `SubmitAsync(CancellationToken)` 和 `SubmitAsyncCommand`。完整实例见 [认证表单](../../sample/MiKiNuo.Mvi.Samples.Avalonia/Features/V2Auth/README.md)。

`[RequestHandler]` 声明生成契约端口，宿主经 `Mediator` 选择目标。生成的 `CreateAsync(IServiceProvider)` 复用 `FeatureFactory.CreateAsync<TFeature>`，使用标准 DI 创建每实例独立 Scope；直接 `new` 的外部服务仍由外部持有。构造失败会关闭实际构造目标并等真实退出后清理，不捕获同类型依赖或其他外部实例。

生成器使用真实 Roslyn 符号与增量输出比较；非法签名、声明歧义及成员冲突定位原声明。消费者诊断为 MVI2001–MVI2016，英文命名和无中文 XML 的外部消费者可以正常使用。仓库自身的明确类型、格式和中文公共 XML 标准见 [AGENTS.md](../../AGENTS.md)。

## 提交与执行

1. 输入、操作调用、目标消息和内部反馈进入同实例 Store。
2. 在短原子区间核对生命周期、操作配置、启动验证和当前输入；启动 IO 的快照就是通过验证的快照。
3. 纯 Reduce 计算下一快照与决定；故障保留原快照，拒绝不产生执行 Effect。
4. Store 提交 State、Operations 和单调 Version，再在门外调度本地展示。
5. `ExecuteAsync` 执行已接纳业务；IO 不持提交门，期间其他合法编辑可继续提交。
6. `Operation.UpdateAsync` 把纯转换及载荷交给 `FeedbackIntent`，按当前 State 转换；执行完成也回流并提交运行事实。

CanExecute 提供展示反馈，实际启动验证是权威条件。可见拒绝原因来自已提交快照；重复尝试的拒绝不清空已有执行的 IsRunning。没有变化的输入或拒绝不必增加版本。

启动与反馈的验证各有用途。反馈核对实例关闭、精确操作身份、Latest 有效性及取消/故障；不再次用旧启动规则拒绝合法完成。旧执行反馈无法覆盖新执行，也不能据拒绝写回宣称外部 IO 已撤销。

Operation.Snapshot 是启动输入。UpdateAsync 的转换参数是提交时的当前 State，用于保留无关新编辑。Completed 等待保证有关状态已提交，UI 绘制可稍后发生；取消不回滚此前提交。

![登录时序](diagrams/login-sequence.png)

## 并发与结果

| 策略 | 接纳行为 |
| --- | --- |
| Reject（默认） | 同实例同操作已有执行时拒绝重复 |
| Latest | 新操作身份使旧执行失效并请求协作取消，迟到反馈拒绝写回 |
| Queue | 正数等待容量，运行项不占等待容量；按处理时当前 State 验证并顺序执行 |
| Parallel | 正数并行上限，达到上限明确拒绝 |

不同操作可以并行等待 IO；状态提交仍按实例有序。业务冲突由纯规则和外部服务负责，不提供跨时间或跨 Feature 自动事务。

`OperationResult<TResult>` 区分 Completed、Rejected、Canceled、Superseded、Faulted，并提供原因、关联身份和强类型业务值。正常方法返回的业务失败仍是 Completed。`RequestResult<TResult>.Kind` 另外表达 WaitCanceled；它属于调用者等待，不能冒充目标停止或成功。

![Latest 时序](diagrams/latest-sequence.png)

## 组合与定向通信

完整 Feature 可以同类型多开、嵌套和动态加入。`parent.Children.Add(child)` 创建所有权成员，`FeatureMember.Wire` 将其请求端口接入宿主 Mediator。明确目标可以直接发送，自动唯一目标可用，缺失或歧义返回可识别结果；视觉挂载不自动改变所有权或扩展路由可见性。

![实例组合](diagrams/composition-instances.png)

SendAsync 等待目标处理与提交。Post/TryPost 只取得定向消息接纳，`PostReceipt<TResult>.Completion` 表示其后续处理，验证拒绝或执行故障继续与原 receipt 相关。一个实例共用有界 Post FIFO，容量包含全部未终结 Post；关闭或满载明确拒绝。

接纳前取消不启动服务。接纳后默认 TargetOwned，由目标管理执行；Propagate 端口明确接受执行取消令牌。等待令牌与执行令牌各自表达真实结果。旧发起操作通过框架发送后续消息时仍须有效。

Mediator 的注册是宿主建立明确契约路由，不是观察者订阅。业务没有 Publish/Subscribe、通知主题或跨 Feature 状态观察图。整体业务保存由协调操作分别定向调用目标，后端事务由应用契约处理。

![中介者时序](diagrams/mediator-sequence.png)

## View 与关闭

Avalonia 使用 `AvaloniaProjection`、平台 Dispatcher 和 `ContentControl` 的 `AvaloniaFeatureHost`；Godot 使用 `GodotProjection` 的主线程帧队列、原生 Control/Node 信号和 `GodotFeatureHost`。后台状态只入平台队列，原生控件在 UI/SceneTree 主线程更新。

默认 `ProjectionMode.Coalesce` 合并待展示快照，只展示最新值；`EveryCommit` 可保留中间展示。合并只影响展示，不丢业务输入、定向消息、导航或一次性提示。平台本地 PropertyChanged/原生信号由 View 连接管理，不为业务建立逐字段框架订阅。

同实例稳定挂载不重复连接；Unmount/替换释放旧 View，旧回调及旧命令失效，实例状态按所有权保留。Godot 移除 Host 自有 View 节点，保留无关节点；闭实例无法重连业务。

`RequestCloseAsync` 返回 `CloseRequestResult`：先确认整个所有权范围，任一子实例拒绝时整体保持可用，其他 IO 和服务没有提前取消或释放。确认期间 State/成员变化使旧确认失效，最终在同一整树提交边界重验条件。

逻辑关闭停止新输入与迟到反馈、退出活动成员和路由，再请求取消。`Close()` 返回 `CloseResult` 和 `CloseTicket`；关闭成功请求也提供该关闭结果。`CloseTicket.Released` 表示所有所属执行真实退出后的资源释放结果。已退休子实例仍由父级跟踪，超时或取消等待不伪报 Released，不强制释放仍被 IO 使用的 Scope。

导航关闭发起者时响应先返回，避免等发起者退出的等待环。框架跟踪活动消息依赖与执行上下文，释放自等待给明确错误；所属额外工作必须被 await 或 `Operation.Track` 跟踪。真实释放失败通过 `ReleaseResult.Exception` 观察。

## 默认入口与交付证据

无参数 Avalonia 启动为三个 v2 认证表单；其他输入/Latest/重挂载/组合窗口由显式参数选择。Godot 示例同时包含 HUD 和真实异步组合。v1 默认 AppComposition、角色目录、六个旧源项目及旧 Benchmark 从当前树退役；固定归档仍包含完整 v1 代码、测试、Benchmark 和中央配置。

正式、preview 与本地共用 `scripts/pack-local.ps1`，其调用的 `verify-package-consumers.ps1` 维护唯一三包清单，fresh 打包后用四个仓库外消费者证明编译/运行与原声明诊断。默认本地门禁包含两平台真实窗口；普通托管 CI 使用明确的 NonGraphical 子集，结果不会声称 GUI 已运行。

UML、Mermaid 和交互图表达协议；生成与浏览器校验见 [图源说明](diagrams/README.md)，这类校验不证明运行行为。适用 T01–T21、真实平台与包证据以 20 票据为准；01–19 历史通过不替代本次候选重验。

性能验收比较等价手写实现、固定归档 v1 与 v2，创建/Scope 增加标准 DI 对照。21、22 将记录环境、固定负载、预热、重复样本、分配与内存、真实 UI 和生成编译成本，再建立预算与回归结论。稳定泄漏、错写、丢事件或取消误判直接失败；当前功能通过不预先宣称性能达标。
