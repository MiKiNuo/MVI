# MiKiNuo.Mvi

基于 .NET 10、R3、Source Generator 和编译期 DI 的组合式 MVI 框架，提供 Avalonia 与 Godot 平台适配。

## 当前架构

```text
View → ViewModel → Intent → Middleware → Handler → Service / Mediator
                                            ↓
                                         Mutation
                                            ↓
View ← ViewModel ← States ← State ← Store + Reducer
```

- **Intent** 捕获用户或组件输入；**Handler** 编排业务和异步外部调用。
- **Mutation** 描述本组件的状态变化；**Reducer** 只根据旧 State 和 Mutation 返回新 State，不执行 I/O，也不原地修改旧对象。
- **Store** 是业务状态提交入口；Handler 通过 `IIntentContext<TState>.Reduce` 或 `TryReduce` 提交 Mutation。
- **ViewModel** 负责状态投影、输入草稿和命令；**View** 负责平台绑定与展示。
- **Middleware** 包裹 Intent 处理，负责日志、校验等横切逻辑；**Mediator** 协调不同 Feature。

当前协议使用 `MviStore<TState, TIntent>`、`MviReducerBase<TState>` 和 `MviViewModelBase<TState, TIntent>`。不使用旧版三泛型 Store 或 EffectDispatcher 协议。

### 状态边界

业务状态由 Store 持有。`CurrentState` 提供最近提交的快照，`States` 是不暴露写入能力的 R3 `Observable<TState>`；新订阅者取得最近已发布状态。框架不深拷贝 State，调用方必须保证整个对象图不会被原地修改。

推荐使用只读 record、`init` 属性和 `System.Collections.Immutable` 集合，并由 Reducer 用 `with` 创建新值。`record` 本身不代表深度不可变：其中的 `List<T>`、数组和可变对象仍可能破坏历史快照。

分析器 **MVI0204** 对 State 公开 setter、可写字段及常见可变集合发出警告。该检查不证明任意对象图深度不可变，也不保证 Reducer 无副作用；只读集合接口背后的可变对象、自定义方法和嵌套对象仍需业务约束及测试。

样例中的 `UserName`、`Password` 是 ViewModel 本地输入草稿，并非 Store 业务状态。提交命令捕获草稿构造 Intent；标记 `Sensitive = true` 的字符串草稿会在提交时清空，但这不等于擦除托管内存中的字符串。需要可回放的逐键编辑时，应显式声明对应 Intent 和状态投影，不能直接修改 State。

### 并发、取消和释放

- Handler 可以并发执行；Reducer 提交在 Store 锁内串行完成。不能假设 Intent 的完成顺序等于派发顺序。
- `TryReduce(guard, mutation)` 在同一个锁内判断当前状态并提交，适用于忙碌保护等原子检查。不要先读取 `context.State` 再假定后续提交仍基于同一版本。
- 锁不会跨越异步等待或观察者回调；状态按提交顺序发布，重入派发不会打乱该发布顺序。
- `DispatchAsync` 等待该 Handler 的完整工作；`TryPost` 仅报告队列是否接纳，后续失败通过 `Errors` 观察。
- Handler 返回后 Context 失效。调用方取消时仍允许 Handler 在 `finally` 中提交清理 Mutation；Store 停止后拒绝继续提交。
- `DisposeAsync` 停止准入、发出取消并等待已准入操作及通知工作结束。无法强制终止不响应取消的服务；宿主关闭时应异步等待，不在 UI 线程调用 `.Wait()` 或 `.Result`。

## 项目结构

| 项目 | 职责 |
| --- | --- |
| `src/MiKiNuo.Mvi.Abstractions` | State、Intent、Mutation、DI 等基础契约 |
| `src/MiKiNuo.Mvi.Runtime` | Store、Handler、Reducer、Middleware、Mediator、Feature 生命周期 |
| `src/MiKiNuo.Mvi.Binding` | 平台无关 ViewModel、Command、事件绑定和 UI 调度抽象 |
| `src/MiKiNuo.Mvi.Generators` | 源生成器、分析器和编译期 DI 工厂 |
| `src/MiKiNuo.Mvi.Platforms.Avalonia` | Avalonia 调度及 View 适配 |
| `src/MiKiNuo.Mvi.Platforms.Godot` | Godot 节点调度及 View 适配 |
| `sample/MiKiNuo.Mvi.Samples.Avalonia` | 登录、注册、重置密码、主页与组合示例 |
| `test/MiKiNuo.Mvi.Tests` | TUnit 回归测试 |
| `test/MiKiNuo.Mvi.Benchmarks` | 基准场景 |

运行时依赖方向为 `Platforms → Binding → Runtime → Abstractions`。Generators 是编译期工具；仓库项目通过 `UseMiKiNuoMviBuildTimeTools` 和 `Directory.Build.targets` 引入，不作为业务运行时服务。

## 最小 Store 示例

以下代码展示核心接口，不依赖 UI 或源生成器生成业务分发代码，可放入引用 Runtime 的 .NET 10 控制台项目。

```csharp
using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using R3;

await using var store = new MviStore<CounterState, IncrementIntent>(
    new CounterState(0), new CounterHandler(), new CounterReducer());
using var subscription = store.States.Subscribe(state => Console.WriteLine(state.Count));
await store.DispatchAsync(new IncrementIntent());

/// <summary>计数状态。</summary>
/// <param name="Count">当前计数。</param>
public sealed record CounterState(int Count) : IMviState;
/// <summary>用户递增意图。</summary>
public sealed record IncrementIntent : IMviIntent;
/// <summary>计数增加变化。</summary>
public sealed record IncrementMutation : IMviMutation<CounterState>;

/// <summary>编排递增操作。</summary>
public sealed class CounterHandler : IIntentHandler<IncrementIntent, CounterState>
{
    /// <summary>提交递增变化。</summary>
    public ValueTask HandleAsync(IncrementIntent intent, IIntentContext<CounterState> context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Reduce(new IncrementMutation());
        return ValueTask.CompletedTask;
    }
}

/// <summary>纯计数转换。</summary>
public sealed class CounterReducer : IMviReducer<CounterState>
{
    /// <summary>生成新的计数快照。</summary>
    public CounterState Reduce(CounterState state, IMviMutation<CounterState> mutation)
        => mutation is IncrementMutation
            ? state with { Count = state.Count + 1 }
            : throw new ArgumentException("不支持的变化。", nameof(mutation));
}
```

实际 Feature 可用 `[MviHandle]` 生成 Handler 分发、`[MviReduce]` 生成纯 Reducer 分发、`[MviBind]` 和 `[MviCommand]` 生成绑定。完整声明见 [LoginHandler](sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/LoginHandler.cs)、[LoginReducer](sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/LoginReducer.cs) 和 [LoginViewModel](sample/MiKiNuo.Mvi.Samples.Avalonia/Features/Login/LoginViewModel.cs)。

## Feature 组合与平台生命周期

- `[MviFeature]` 标记 Handler；`[MviComposition(typeof(...Handler), ...)]` 声明组合成员。每个 Feature 实例拥有独立 Handler、Reducer、Store 和 ViewModel。
- 组合根通过生成的异步工厂创建实例，例如 `Container.CreateAppCompositionAsync()`；见 [SampleCompositionRoot](sample/MiKiNuo.Mvi.Samples.Avalonia/Composition/SampleCompositionRoot.cs)。
- `MviCompositionScope` 定义通信范围，以实例端点、显式路由和 `IMviMediator` 协调请求。通知接纳成功不等于后续业务完成，不跨范围自动广播。
- `Own` 建立所有权树；组合的 `DisposeAsync` 负责关闭所拥有实例。跨 Store 的事务或部分失败策略由具体业务定义。
- View 的绑定释放与业务 Feature 的关闭是两件事；视图离树不等于销毁业务实例。ViewModel 借用 Store，不负责释放它。
- Avalonia 宿主传入 `AvaloniaMviUiDispatcher`，窗口关闭时等待组合释放，见 [MainWindow](sample/MiKiNuo.Mvi.Samples.Avalonia/MainWindow.axaml.cs)。
- Godot 宿主在主线程调用 `GodotMviBootstrapper.Install(root)` 并将返回实例作为 `IMviUiDispatcher` 注册。`Post` 只入队；节点就绪及后续主线程帧负责执行，挂载前或离树后暂不执行，重新挂载后继续处理。永久销毁节点前应先结束业务生产方。
- Godot 首次属性或按钮刷新失败时，已注册订阅随本次绑定清理，避免残留事件处理器。

仓库当前没有独立 `project.godot` 样例工程。Godot 平台源码编译和托管边界回归测试不能替代目标引擎中的场景集成验证。

## 本地构建与运行

需要 .NET 10 SDK，具体选择遵循 `global.json`。打开 `MiKiNuo.Mvi.slnx`，或执行：

```bash
dotnet restore MiKiNuo.Mvi.slnx
dotnet build MiKiNuo.Mvi.slnx --no-restore
dotnet run --project test/MiKiNuo.Mvi.Tests --no-build
```

测试项目使用 TUnit / Microsoft.Testing.Platform，已配置 `--maximum-parallel-tests 1`，无需再次传入同一参数。

完整构建脚本会依次还原、编译、运行测试并列出基准场景，日志在 `artifacts/`：

```bash
bash build.sh
```

```powershell
./build.ps1
```

基准列举只验证发现能力，不代表已执行性能测量。

启动 Avalonia 示例：

```bash
dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia
```

账号服务使用 DummyJSON 公共演示 API，需要网络；注册与改密仅模拟写入，不会持久保存账号或修改真实密码。不要输入真实密码或个人信息。HTTP 契约测试使用受控传输，不依赖公网可用性。

## 本地打包

平台使用方按目标选择 `MiKiNuo.Mvi.Platforms.Avalonia` 或 `MiKiNuo.Mvi.Platforms.Godot`；非 UI 使用方可引用 `MiKiNuo.Mvi.Runtime`。完整包清单与依赖见 [eng/packages.json](eng/packages.json)，本地打包和验证入口为 `scripts/pack-local.ps1` / `scripts/pack-local.sh`，需要 Python 3.10+。

## License

MIT
