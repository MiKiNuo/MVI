# ViewEvent 到 Command 绑定设计决策

读者：未来实现或维护 MVI 跨平台事件绑定的框架维护者。

读完后应能做的事：按本文约束实现第一版 `ViewEvent -> ICommand -> Intent` 链路，并判断新增事件类型是否应该进入跨平台主路。

## 背景

当前 MVI 的命令模型主要覆盖按钮、菜单等显式操作。ViewModel 通过命令属性暴露用户意图，Source Generator 负责把命令执行转换为 Intent 派发。Avalonia 可以直接使用原生 Command 绑定，Godot 目前通过平台 View 基类提供按钮绑定 helper。

这套模型对按钮足够清晰，但 View 自带事件不只有按钮点击。文本变化、选择变化、指针输入、生命周期事件等都可能需要进入 MVI 数据流。如果每个平台都手写事件转发，业务交互会重新耦合到具体 UI 框架；如果把所有平台事件直接暴露给业务层，跨平台边界会被破坏。

## 目标

第一版设计采用一条统一链路：

```text
平台 View 事件
  -> 平台适配层转换为跨平台 payload
  -> ICommand.Execute(payload)
  -> Command 根据 payload 构造 Intent
  -> Store.Dispatch
  -> Reducer / Effect
```

核心目标是保持 ViewModel 的绑定面稳定：ViewModel 只暴露属性和命令，平台事件名、控件实例、订阅和解绑生命周期都留在 View 层。

## 非目标

第一版不做完整 UI 事件框架，不试图强类型封装所有平台事件。

第一版不把事件绑定层变成结果回调通道。事件执行后的 UI 变化仍然通过 State 或 Effect 回流。

第一版不做 payload 字段自动拆包。Intent 构造必须显式、可预测。

## 包边界

跨平台 payload 类型和 ViewEvent 绑定契约属于表现层语义，应放在 `MiKiNuo.Mvi.Presentation`。平台包负责把 Avalonia、Godot 等原生事件转换成这些类型。

`MiKiNuo.Mvi.Domain` 保持纯 MVI 标记接口和公共基础模型，不承载 UI 事件语义。

`MiKiNuo.Mvi.Application` 继续承载 Command、Store、ViewModel Base 等运行时核心，不直接依赖具体 UI 平台。

## View 层声明事件绑定

事件绑定声明放在 View 层。ViewModel 不声明平台事件名，也不认识控件实例。

Avalonia 第一版以 XAML attached behavior 为主，例如：

```xml
<TextBox
    mvi:ViewEvents.TextChangedCommand="{Binding SearchChangedCommand}"
    mvi:ViewEvents.TextChangedDebounce="00:00:00.300" />

<ListBox
    mvi:ViewEvents.SelectionChangedCommand="{Binding SelectPatientCommand}" />

<Border
    mvi:ViewEvents.PointerPressedCommand="{Binding OpenCardCommand}" />
```

按钮仍可继续使用 Avalonia 原生 Command 绑定。需要统一事件模型时，也可以提供 `ActionCommand` attached behavior。

Godot 第一版提供统一 helper，并保留常用快捷方法。主入口使用事件订阅和解绑委托，避免把 Godot signal 名称扩散到业务层：

```csharp
BindEvent(
    handler => button.Pressed += handler,
    handler => button.Pressed -= handler,
    command,
    bindings,
    payloadFactory,
    button.Name);

BindEvent<string>(
    handler => lineEdit.TextChanged += handler,
    handler => lineEdit.TextChanged -= handler,
    command,
    bindings,
    text => new MviTextChangedEventPayload(text),
    lineEdit.Name);
```

`BindButton` 等快捷方法内部应复用统一事件绑定逻辑，避免 Godot signal 增多后 helper 爆炸。

## Payload 模型

payload 是 UI 事件发生时随事件携带的数据。框架主路使用少量平台无关 payload 类型，并保留 `RawEventArgs` 逃生口支持平台特殊事件。

第一版正式支持四类强类型 payload。

`Action` 用于按钮点击、菜单执行等显式操作。payload 字段保持轻量：

- `SourceName`
- `ActionName`
- `RawEventArgs`

无参按钮命令仍然允许不使用 payload。

`TextChanged` 用于文本输入变化：

- `Text`
- `PreviousText`
- `IsUserInitiated`
- `RawEventArgs`

普通文本输入默认可以每次变化派发轻量状态 Intent。搜索、远程校验、保存草稿等昂贵行为必须显式使用 Debounce、提交事件或其它业务策略。

`SelectionChanged` 用于列表、下拉、菜单选择：

- `SelectedValue`
- `SelectedIndex`
- `PreviousSelectedValue`
- `RawEventArgs`

`SelectedValue` 允许是 `object?`，但业务代码应优先传稳定 id 或 ViewModel item，避免传平台控件对象。这样 Reducer、测试和跨平台复用不会依赖 Avalonia 或 Godot 的控件类型。

`Pointer` 用于基础鼠标、触摸或指针输入：

- `PositionX`
- `PositionY`
- `Button`
- `ClickCount`
- `IsPressed`
- `Modifiers`
- `RawEventArgs`

第一版不抽象压力、倾斜、触摸 id、滚轮 delta、手势等高级输入。它们可以通过 escape hatch 临时处理，等真实需求稳定后再进入主路。

## Command 到 Intent

现有命令属性和 `MviCommandAttribute` 仍然是 ViewModel 对外绑定面。Source Generator 继续负责 `Command -> Intent`。

Intent 构造规则必须窄而明确：

```text
payload == null
  -> 优先使用 Intent()

payload != null
  -> 优先使用唯一的 Intent(TPayload payload)
  -> payload 类型必须可赋值给 TPayload
```

如果 Intent 同时存在多个一参构造函数，必须在命令元数据上显式指定 payload 类型。否则 Source Generator 应报告 diagnostic，不靠构造函数顺序猜测。

第一版不支持自动把 payload 字段拆包到多参数 Intent。例如不自动把 `payload.Text` 映射到 `Intent(string text)`。需要文本值时，应定义 `Intent(MviTextChangedEventPayload payload)` 或由开发者手写命令/factory。

## 运行时行为

事件触发时始终先检查 `CanExecute`。不能执行时不派发 Intent。

只有具备明确禁用语义的 Action 控件自动回写启用状态，例如按钮的 enabled/disabled。`TextChanged`、`SelectionChanged`、`Pointer` 不自动禁用控件，避免输入框、列表或点击区域出现反直觉行为。

绑定错误采用开发期快速失败：

- 事件名写错时抛清晰异常。
- 手写 helper 入口的 Command 为 null 时抛清晰异常；XAML attached behavior 的 Command 为 null 时视为暂未绑定，以兼容 DataContext 尚未就绪的阶段。
- payload 和 Intent 构造函数不匹配时由 Source Generator 优先报告 diagnostic；无法静态验证的问题在运行时抛清晰异常。

事件绑定层不回传执行结果。异常进入统一诊断、平台日志或快速失败路径。

## 高频事件

默认不偷偷节流。每次事件都会检查 `CanExecute` 并执行命令。

第一版实现 `Debounce` 作为绑定级选项，优先服务文本搜索、远程校验等输入场景。`Throttle` 暂不进入第一版。

View 解绑或销毁时，pending debounce 事件直接丢弃，并取消 timer/task。解绑后不能继续执行 Command。

异步命令第一版保持现有默认行为：允许并发执行。后续可以在绑定级或命令级增加 `IgnoreWhileRunning`、`CancelPrevious` 等策略，但不作为第一版硬约束。

## 第一版范围

第一版正式支持：

- Action
- TextChanged
- SelectionChanged
- Pointer
- RawEventArgs escape hatch
- Avalonia attached behavior
- Godot 统一 BindEvent helper 和常用快捷方法
- Source Generator 对 payload Intent 构造的窄规则支持
- 绑定失败快速失败和可静态验证 diagnostic
- Debounce 绑定选项

第一版暂不正式支持：

- Keyboard
- Focus
- Scroll
- Drag / Drop
- Lifecycle
- Pointer move 高频优化
- payload 字段自动拆包
- 事件执行结果回传
- 完整手势模型

这些事件类别可以通过 escape hatch 临时接入。只有当跨平台语义、测试需求和样例都稳定后，才进入强类型 payload 主路。

## 实施清单

实现时按以下顺序推进：

1. 在表现层定义 payload 类型、事件选项和共享契约。
2. 扩展命令生成器，让 payload 构造 Intent 的规则可静态验证。
3. 为构造函数歧义、payload 不匹配、空命令等情况补 diagnostic 或清晰异常。
4. 实现 Avalonia attached behavior，覆盖 Action、TextChanged、SelectionChanged、Pointer 和 Debounce。
5. 实现 Godot 统一事件绑定 helper，并让按钮快捷方法复用它。
6. 更新样例，展示文本输入实时状态同步和昂贵操作 Debounce 的区别。
7. 增加单元测试和平台适配测试，覆盖解绑丢弃 pending debounce、CanExecute 行为、payload 到 Intent 构造规则。
