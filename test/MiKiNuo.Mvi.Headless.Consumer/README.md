# 无界面状态消费者

运行 `dotnet run --project test/MiKiNuo.Mvi.Headless.Consumer -c Release`。消费者仅引用核心运行时；生成器引用使用 `OutputItemType="Analyzer"` 与 `ReferenceOutputAssembly="false"`。

作者声明不可变 State 和直接继承 `Feature<TState>` 的顶层、非泛型 partial 类。`[Input]` 仅用于 public 实例属性的 public get/init；框架生成 `Set<Property>(value)`。未标记属性没有输入入口。`[OnInput(nameof(State.Property))]` 规则必须是精确的 `private static State Method(State state, PropertyType value)`，只替换这个属性的默认 with 回写；规则必须保持纯转换。

当前支持在消费者源码中声明的密封 record 类、readonly record struct 及递归不可变成员，包括基础值类型、字符串、枚举、Guid、日期/时间、Nullable，以及元素满足相同约束的 ImmutableArray、ImmutableList、ImmutableQueue、ImmutableStack。继承成员也接受验证。其他程序集中的未知记录及其继承链会被拒绝，因为默认元数据导入不能证明其私有存储不可变；明确支持的 BCL 标量与不可变集合不受此限制。普通类、未密封记录、可写 setter、显式实例字段、数组、可变集合、只读集合包装或接口、其他尚未验证的类型会被声明诊断拒绝；不能仅凭 record 或 IReadOnlyList 认定不可变。

Snapshot 同时提供 State、Version 和不可变 OperationStates。当前只实现纯状态闭环，OperationStates 始终为空，没有操作状态字符串的写入入口。成功输入在实例短提交门中依据当前 State 计算并提交单调版本；规则抛异常、返回 null 或重入同一实例时不提交快照。输入允许空字符串等中间值；业务操作准入协议由后续票据实现。
