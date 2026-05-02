# MiKiNuo.Mvi

> 一套面向复杂业务 UI 的 **响应式 MVI 架构框架**。  
> 使用 **.NET 10 + R3 + Source Generator + Clean Architecture + Analyzer + 编译期 DI**，让复杂界面的状态流转、组件通信、业务副作用和 UI 绑定变得清晰、可测试、可复用。

---

## 1. 为什么需要 MiKiNuo.Mvi？

在很多 .NET 客户端项目中，尤其是 HIS、EMR、LIS、MES、ERP、WMS、桌面管理系统、工业上位机、游戏 UI 等复杂业务系统里，界面往往不是一个简单页面，而是由大量子区域、子组件、业务表单、状态面板、流程节点共同组成。

传统开发方式很容易演变成下面这样：

```text
View 里写事件
ViewModel 里写业务判断
Command 里调接口
属性 setter 里改状态
子 ViewModel 互相引用
页面之间直接传对象
事件中心到处 Publish / Subscribe
```

项目越做越大后，常见问题会越来越明显：

- ViewModel 越写越厚，既负责绑定，又负责业务，又负责调服务。
- 状态变化来源很多，很难判断是谁改了界面。
- 一个按钮点击后会影响多个子界面，但数据流不清晰。
- 父界面和子界面、子界面和子界面之间互相引用。
- 页面复用困难，患者检索、审计时间线、表单状态等能力反复复制。
- DI 注册、View 注册、Reducer 分发、Command 绑定全是重复代码。
- UI 框架绑定机制和业务状态流混在一起，单元测试困难。
- 想跨 Avalonia、WinForms、Unity、Godot 复用逻辑时，发现代码都绑死在具体 UI 框架里。

`MiKiNuo.Mvi` 的目标就是解决这些问题：  
**用 MVI 单向数据流管理复杂 UI 状态，用 R3 驱动响应式变化，用 Mediator 协调组件通信，用 Source Generator 消除样板代码，用 Analyzer 防止架构腐化。**

---

## 2. MVI 是什么？

MVI 是 **Model-View-Intent** 的缩写。

在本项目中，MVI 可以理解为一种更适合复杂交互界面的状态管理模型：

```text
View
  -> Intent
  -> Middleware
  -> Reducer
  -> State
  -> View
```

如果存在副作用，则走：

```text
Reducer
  -> Effect
  -> EffectDispatcher
  -> Service / Mediator / Async Task
  -> New Intent
```

核心思想是：  
**界面不能随便改状态，所有变化都必须通过 Intent 进入 Store，再由 Reducer 生成新的 State。**

---

## 3. 为什么选择 MVI，而不是传统 MVVM？

MVVM 非常适合简单表单和中小型页面，但在复杂业务系统中，传统 MVVM 往往会遇到几个难点。

### 3.1 MVVM 容易让 ViewModel 变胖

传统 MVVM 中，ViewModel 通常同时负责：

- 属性绑定
- Command
- CanExecute
- 表单校验
- 接口调用
- 页面跳转
- 状态切换
- 子模块通信
- 错误处理

久而久之，ViewModel 会变成“上帝对象”。

MVI 将这些职责拆开：

| 职责 | MVI 中的归属 |
|---|---|
| UI 显示 | View |
| UI 可绑定属性和命令 | ViewModel |
| 用户行为 | Intent |
| 状态转换 | Reducer |
| 异步请求、导航、外部调用 | EffectDispatcher |
| 横切逻辑 | Middleware |
| 组件协调 | Mediator |
| 当前界面数据 | State |

ViewModel 不再承载业务逻辑，只是 View 的绑定适配层。

---

### 3.2 MVVM 的状态变化来源不够统一

在传统 MVVM 中，状态可能来自：

```text
属性 setter
Command
服务回调
事件订阅
消息总线
异步任务
子 ViewModel 直接赋值
```

MVI 则要求所有状态变化都回到统一入口：

```text
Intent -> Reducer -> State
```

这带来三个好处：

1. 状态变化可追踪。
2. 业务逻辑更容易测试。
3. 多组件交互时数据流更清晰。

---

### 3.3 MVVM 中组件通信容易变成网状依赖

复杂 Dashboard 通常是这样：

```text
父页面
├─ 子页面 A
├─ 子页面 B
├─ 子页面 C
└─ 子页面 D
```

如果 A 直接引用 B，B 直接引用 C，C 再调用父页面，依赖关系会变成网状。

MVI 中推荐：

```text
子 MVI -> Mediator Request -> 父 MVI 协调 -> 目标子 MVI
```

子组件之间不直接互相引用，而是通过明确的 Request / Response 进行业务协同。

---

## 4. MiKiNuo.Mvi 的核心架构

项目严格采用 Clean Architecture 分层：

```text
src
├─ MiKiNuo.Mvi.Domain
├─ MiKiNuo.Mvi.Application
├─ MiKiNuo.Mvi.Infrastructure
└─ MiKiNuo.Mvi.Presentation
```

| 层 | 职责 |
|---|---|
| Domain | MVI / DI 基础抽象、标记接口、公共模型 |
| Application | Store、Command、Middleware、Mediator、ViewModel Base 等运行时核心 |
| Infrastructure | Source Generator、Analyzer、编译期工具 |
| Presentation | Avalonia、WinForms、Godot、Unity 等平台适配 |

依赖方向：

```text
Presentation -> Application -> Domain
Infrastructure 作为编译期工具参与生成和检查
```

核心层不依赖 Avalonia、WinForms、Unity、Godot。  
这意味着业务状态、Intent、Reducer、Effect、Mediator 协调逻辑可以跨平台复用。

---

## 5. 核心数据流

### 5.1 普通状态更新

```text
View 输入
  -> ViewModel 属性 / Command
  -> Intent
  -> Middleware
  -> Reducer
  -> New State
  -> R3 State Stream
  -> ViewModel
  -> View 刷新
```

### 5.2 副作用处理

```text
Intent
  -> Reducer
  -> Effect
  -> EffectDispatcher
  -> API / Mediator / Navigation / Async Task
  -> New Intent
  -> Reducer
  -> New State
```

### 5.3 父子组件通信

```text
子 MVI
  -> Effect
  -> EffectDispatcher
  -> Mediator.SendAsync(...)
  -> 父 MVI 协调
  -> 目标子 MVI Dispatch Intent
```

### 5.4 兄弟组件通信

```text
子组件 A
  -> Mediator Request
  -> 父级协调器
  -> 子组件 B Dispatch Intent
  -> 子组件 B 更新 State
```

这种方式避免事件中心式的隐式广播，也避免子组件之间直接引用。

---

## 6. R3 在项目中的作用

本项目不依赖 ReactiveUI，也不依赖 Rx.NET 或其他响应式框架，只使用 R3。

R3 在框架中承担核心响应式能力：

| 场景 | R3 作用 |
|---|---|
| Store 状态变化 | `Observable<TState>` 推送新状态 |
| Effect 流转 | `Observable<TEffect>` 推送副作用 |
| Command 可执行状态 | `Observable<bool>` 驱动 CanExecute |
| ViewModel 回写 | State Stream 驱动属性更新 |
| 中间件诊断 | 记录 Intent、Reducer、Effect、耗时日志 |
| 异步副作用 | 与 `ValueTask`、取消标记配合 |
| UI 通知调度 | 将状态变化安全投递回 UI 线程 |

View 对外仍然使用 UI 框架能识别的绑定协议，例如：

```xml
<TextBox Text="{Binding UserName, Mode=TwoWay}" />
<Button Command="{Binding SubmitCommand}" />
```

但内部不是传统 MVVM 的 setter 直接改业务，而是：

```text
TextBox 输入
  -> ViewModel setter
  -> Intent
  -> Store
  -> Reducer
  -> State
  -> R3 State Stream
  -> ViewModel
  -> View
```

R3 的价值在于：  
**让状态、命令、副作用和诊断日志都变成清晰的响应式流，而不是散落的事件回调。**

---

## 7. Source Generator：少写样板代码

复杂 UI 框架最大的痛点之一是重复代码太多。

传统 ViewModel 需要写：

- backing field
- PropertyChanged
- Command
- CanExecute
- setter
- 状态同步
- Intent 派发
- Dispose

本项目通过 Source Generator 自动生成这些代码。

开发者只需要声明绑定关系：

```csharp
[MviBind(
    StateProperty = nameof(LoginState.UserName),
    BindingMode = MviBindingMode.TwoWay,
    IntentType = typeof(LoginIntent.ChangeUserName))]
public partial string UserName { get; set; }
```

生成器会自动生成：

```text
属性 backing field
PropertyChanged 通知
双向绑定 setter
Intent 派发
ApplyStateCore
Command 初始化
CanExecute 订阅
Dispose 释放
```

同时还会生成：

```text
Reducer Dispatcher
DI Container
ViewRegistry
部分组合注册代码
```

这带来的价值：

- 减少重复代码。
- 避免手写分发 switch。
- 避免运行时反射。
- 让架构约束前移到编译期。
- 让 ViewModel 代码保持干净。

---

## 8. 编译期 DI：不靠运行时扫描

项目中的 DI 不是传统运行时扫描注册，而是编译期生成强类型工厂。

目标：

```text
不使用反射创建对象
不依赖字符串查找
不在运行时扫描程序集
生成强类型工厂代码
支持 Store / ViewModel / ReducerDispatcher / EffectDispatcher / ViewRegistry
```

这样可以减少启动时开销，也能让依赖关系更早暴露在编译期。

---

## 9. Middleware：把横切逻辑从业务中拿出来

Middleware 用于在 Intent 进入 Reducer 前处理横切逻辑。

适合放入中间件的能力：

- 表单校验
- 权限判断
- 防重复提交
- 操作审计
- 日志记录
- 性能统计
- 异常转换
- 业务规则前置拦截

数据流：

```text
Intent
  -> Validation Middleware
  -> Logging Middleware
  -> Performance Middleware
  -> Reducer
```

这样 Reducer 可以保持纯净，只负责状态转换，不需要混入日志、权限、审计等逻辑。

---

## 10. Mediator：真正的协调者，不是事件总线

项目中的 Mediator 不做发布订阅，也不做事件中心。

它采用明确的 Request / Response 模型：

```csharp
await mediator.SendAsync<TRequest, TResponse>(request, cancellationToken);
```

用于解决复杂界面中的组件协同：

```text
子 MVI 提交数据
  -> Mediator
  -> 父 MVI 判断业务规则
  -> 目标子 MVI 更新状态
```

这种模式比事件总线更适合生产业务系统，因为：

- 调用链更明确。
- 不会出现到处订阅。
- 不会出现隐式广播。
- 更容易测试。
- 更容易维护父子组件边界。

---

## 11. Analyzer：让架构不会慢慢腐化

项目内置 Analyzer，重点约束三类问题：

### 11.1 Clean Architecture 分层引用

禁止错误依赖：

```text
Domain -> Application
Domain -> Infrastructure
Domain -> Presentation
Application -> Infrastructure
Application -> Presentation
src -> sample
```

### 11.2 微软 C# 编码规范

强制：

- 公共 API 命名规范。
- 可访问性修饰符。
- IDisposable 释放规范。
- 参数判空。
- Nullable 规范。
- 避免危险命名。

### 11.3 中文 XML 注释

要求类、接口、方法、属性、字段、常量等公共成员具备中文 XML 注释。

注意：  
项目不会把所有语法糖偏好都变成错误，例如是否使用主构造函数、是否使用集合表达式，这些不会作为强制架构规则。

---

## 12. 解决的核心痛点

| 痛点 | 解决方式 |
|---|---|
| ViewModel 臃肿 | ViewModel 只做绑定，业务进入 Intent / Reducer / Effect |
| 状态不可追踪 | 所有变化通过 Intent -> Reducer -> State |
| 多组件通信混乱 | 使用 Mediator 明确协调 |
| 事件总线难维护 | 不使用发布订阅式事件中心 |
| 样板代码过多 | Source Generator 自动生成 |
| DI 注册重复 | 编译期 DI 生成强类型工厂 |
| UI 与业务耦合 | Core 不依赖具体 UI 框架 |
| 复用困难 | 抽象可复用 MVI Feature |
| 架构容易腐化 | Analyzer 编译期约束 |
| 测试困难 | Reducer、Middleware、Effect、Mediator 均可独立测试 |

---

## 13. 如何使用

### 13.1 创建 State

```csharp
public sealed record LoginState(
    string UserName,
    string Password,
    bool CanSubmit,
    bool IsBusy,
    string? ErrorMessage) : IMviState
{
    public static LoginState Initial { get; } = new(
        string.Empty,
        string.Empty,
        false,
        false,
        null);
}
```

### 13.2 创建 Intent

```csharp
public abstract partial record LoginIntent : IMviIntent
{
    public sealed partial record ChangeUserName(string UserName) : LoginIntent;

    public sealed partial record ChangePassword(string Password) : LoginIntent;

    public sealed partial record Submit : LoginIntent;
}
```

### 13.3 创建 Effect

```csharp
public abstract partial record LoginEffect : IMviEffect
{
    public sealed partial record RequestLogin(string UserName, string Password) : LoginEffect;
}
```

### 13.4 编写 Reducer

```csharp
public static partial class LoginReducers
{
    [MviReducer]
    public static MviReduceResult<LoginState, LoginEffect> Reduce(
        LoginState state,
        LoginIntent.ChangeUserName intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LoginState nextState = state with
        {
            UserName = intent.UserName,
            CanSubmit = !string.IsNullOrWhiteSpace(intent.UserName)
                && !string.IsNullOrWhiteSpace(state.Password)
        };

        return MviReduceResult.State(nextState);
    }
}
```

### 13.5 编写 ViewModel

```csharp
public sealed partial class LoginViewModel
    : MviViewModelBase<LoginState, LoginIntent, LoginEffect>
{
    public LoginViewModel(IMviStore<LoginState, LoginIntent, LoginEffect> store)
        : base(store)
    {
    }

    [MviBind(
        StateProperty = nameof(LoginState.UserName),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(LoginIntent.ChangeUserName))]
    public partial string UserName { get; set; }

    [MviBind(StateProperty = nameof(LoginState.CanSubmit))]
    public partial bool CanSubmit { get; }

    [MviCommand(
        IntentType = typeof(LoginIntent.Submit),
        CanExecuteProperty = nameof(CanSubmit),
        IsAsync = true)]
    public partial IMviAsyncCommand SubmitCommand { get; }
}
```

### 13.6 编写 EffectDispatcher

```csharp
public sealed class LoginEffectDispatcher
    : IMviEffectDispatcher<LoginState, LoginIntent, LoginEffect>
{
    public async ValueTask DispatchAsync(
        LoginEffect effect,
        IMviStore<LoginState, LoginIntent, LoginEffect> store,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(store);

        if (effect is LoginEffect.RequestLogin request)
        {
            await store.DispatchAsync(
                new LoginIntent.LoginSucceeded("系统管理员"),
                cancellationToken);
        }
    }
}
```

### 13.7 编写 View

```xml
<TextBox Text="{Binding UserName, Mode=TwoWay}" />
<Button Command="{Binding SubmitCommand}" Content="登录" />
```

View 只负责绑定，不写业务逻辑。

---

## 14. 推荐开发流程

建议按照 TDD 方式开发一个 MVI 功能：

```text
1. 定义 State
2. 定义 Intent
3. 编写 Reducer 测试
4. 实现 Reducer
5. 定义 Effect
6. 实现 EffectDispatcher
7. 声明 ViewModel 绑定属性和命令
8. 编写 View 绑定
9. 添加 Middleware
10. 添加 Mediator 协调
11. 编写端到端数据流测试
```

优先测试：

- Intent 是否正确进入 Reducer。
- Reducer 是否返回正确 State。
- Reducer 是否产生正确 Effect。
- Middleware 是否执行。
- EffectDispatcher 是否派发后续 Intent。
- Mediator 是否协调父子 / 兄弟 MVI。
- ViewModel 双向绑定是否派发正确 Intent。

---

## 15. 本地运行

还原依赖：

```bash
dotnet restore MiKiNuo.Mvi.slnx
```

编译：

```bash
dotnet build MiKiNuo.Mvi.slnx
```

运行测试：

```bash
dotnet test MiKiNuo.Mvi.slnx
```

启动 Avalonia 示例：

```bash
dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj
```

---

