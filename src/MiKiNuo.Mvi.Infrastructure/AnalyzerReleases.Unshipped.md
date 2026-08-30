### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
ARCH0007 | Architecture | Error | Presentation 抽象层禁止引用具体平台项目。
ARCH0008 | Architecture | Error | Infrastructure 源生成器与分析器禁止出现示例项目专属代码（命名空间、类型、字符串）。
ARCH0009 | Architecture | Error | Presentation 抽象层禁止直接引用 Avalonia / Godot 等具体平台 NuGet 包。
MVI0001 | MviBinding | Error | 命令 Intent 存在多个 payload 构造函数时要求显式指定 PayloadType。
MVI0002 | MviBinding | Error | 命令 Intent 缺少指定 PayloadType 对应的一参构造函数。
MVI0003 | MviBinding | Error | 命令 Intent 缺少公开无参构造函数或唯一的一参 payload 构造函数。
MVI0004 | MviReducer | Error | 规约器类必须标记 partial 修饰符，否则源生成器无法 emit Reduce 方法。
MVI0005 | MviReducer | Warning | 意图子类型缺少对应的 [MviReduce] 方法，将走默认分支返回原状态。
MVI0006 | MviReducer | Error | 多个规约方法标记同一意图子类型，每个意图子类型只能有一个规约方法。
MVI0007 | MviReducer | Error | 规约方法签名不符合约定，必须是 (TState, TIntent.Xxx) => MviReduceResult<TState, TEffect>。
MVI0008 | MviReducer | Error | 守卫谓词方法不存在或签名不是 (TState state) => bool。
MVI0009 | MviBinding | Error | 生成器接管的 ViewModel（声明 [MviBind] 或 [MviCommand]）禁止手写 ApplyStateCore 重写，由源生成器实现。
MVI0010 | MviStatePath | Error | 状态属性图存在循环引用，无法展开 StatePath，已跳过该分支。
MVI0011 | MviStatePath | Warning | 泛型状态类型跳过 StatePath 生成。
MVI0012 | MviEffect | Error | 副作用分发器类必须标记 partial 修饰符，否则源生成器无法 emit DispatchCoreAsync 方法。
MVI0013 | MviEffect | Warning | 副作用子类型缺少对应的 [MviEffect] 方法，分发时将走默认分支不执行任何操作。
MVI0014 | MviEffect | Error | 多个处理方法标记同一副作用子类型，每个副作用子类型只能有一个处理方法。
MVI0015 | MviEffect | Error | 副作用处理方法签名不符合约定，必须是 (TEffect.Xxx effect, CancellationToken cancellationToken) => ValueTask。
MVI0016 | MviFeature | Error | Feature 状态类型缺少公开静态 Initial 属性，生成的容器无法构造 Store，已跳过该 Feature 装配。
MVI0017 | MviStatePath | Warning | 禁止使用 StatePath 默认实例（default 字面量 / default(T) / 无参 new），默认实例访问 Getter 会抛 InvalidOperationException。
