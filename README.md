# MiKiNuo.Mvi

`MiKiNuo.Mvi` 是一套基于 **.NET 10 + R3 + Source Generator + Clean Architecture** 设计的响应式 MVI 架构框架，用于构建复杂桌面端、客户端、游戏 UI 和业务系统界面。

项目目标不是做一个简单的 MVVM 示例，而是解决大型业务系统中常见的几个问题：

- ViewModel 代码重复严重。
- 状态变化不可追踪。
- 复杂界面由多个子界面组成后通信混乱。
- 页面之间容易直接引用，耦合越来越高。
- DI 注册、View 注册、Reducer 分发等样板代码越来越多。
- UI 框架绑定机制和业务状态流混在一起，难以测试。
- 多平台 UI 很难复用同一套业务交互逻辑。

本项目通过 **MVI 单向数据流、R3 响应式状态流、真正的 Mediator 协调模式、编译期 Source Generator、高性能 DI、Analyzer 架构约束** 来解决这些问题。

---

## 1. 项目用途

该项目适合用于开发以下类型的软件：

- HIS / EMR / LIS / MES 等复杂业务系统。
- 桌面端管理系统。
- 大型组合式 Avalonia 应用。
- WinForms 老系统重构。
- Unity / Godot 中复杂 UI 状态管理。
- 多页面、多模块、多组件协同的业务前端。
- 需要强架构约束和高可测试性的 .NET 客户端框架。

核心目标是让开发者可以用统一的 MVI 模型描述界面状态、用户意图、副作用和组件通信，而不是把业务逻辑分散写在 View、ViewModel、事件回调和服务调用里。

---

## 2. 架构特点

### 2.1 Clean Architecture 分层

项目源码严格采用四层结构：

```text
src
├─ MiKiNuo.Mvi.Domain
├─ MiKiNuo.Mvi.Application
├─ MiKiNuo.Mvi.Infrastructure
└─ MiKiNuo.Mvi.Presentation
```

分层职责：

| 层 | 职责 |
|---|---|
| Domain | 定义 MVI、DI、公共基础模型和标记接口 |
| Application | 实现 Store、Command、Middleware、Mediator、ViewModel 基类等核心运行时能力 |
| Infrastructure | 实现 Source Generator 和 Analyzer 等编译期能力 |
| Presentation | 实现 Avalonia、WinForms、Godot、Unity 等平台适配 |

依赖方向：

```text
Presentation -> Application -> Domain
Infrastructure 作为编译期工具参与生成和检查
```

核心层不依赖 Avalonia、WinForms、Unity、Godot 等具体 UI 框架，业务交互逻辑可以跨平台复用。

---

### 2.2 响应式 MVI 单向数据流

框架采用 MVI 模式：

```text
View
  -> ViewModel
  -> Intent
  -> Middleware
  -> Reducer
  -> State
  -> ViewModel
  -> View
```

副作用流程：

```text
Reducer
  -> Effect
  -> EffectDispatcher
  -> Mediator / Service / Async Task
  -> New Intent
```

核心概念：

| 概念 | 说明 |
|---|---|
| State | 当前界面状态，必须不可变或可预测更新 |
| Intent | 用户意图，例如输入、提交、刷新、切换页面 |
| Reducer | 纯状态转换逻辑 |
| Effect | 副作用描述，例如登录、导航、请求接口 |
| EffectDispatcher | 执行副作用并派发后续 Intent |
| Store | 管理 State、Intent、Effect 的核心状态容器 |
| Middleware | 在 Intent 进入 Reducer 前进行校验、日志、性能统计、审计 |
| Mediator | 负责父子 MVI、兄弟 MVI、模块之间的协调 |

---

### 2.3 R3 驱动状态和命令

框架不依赖 ReactiveUI，也不依赖其他第三方响应式框架，只使用 R3。

R3 用于：

- State Stream
- Effect Stream
- Intent Dispatch Pipeline
- Command CanExecute Stream
- ViewModel 属性回写
- 中间件诊断日志流
- UI 状态变化通知

View 对外仍然使用 UI 框架能识别的属性和命令，例如 Avalonia 的 Binding 和 ICommand；但 ViewModel 内部的数据流由 R3 驱动。

---

### 2.4 Source Generator 消除重复代码

项目使用 Roslyn Incremental Source Generator 生成重复代码。

生成内容包括：

- ViewModel 属性绑定代码。
- 双向绑定 setter 到 Intent 的派发代码。
- Command 初始化代码。
- Reducer Dispatcher。
- DI 容器工厂。
- ViewRegistry。
- 部分组合注册代码。

开发者只需要写声明式代码：

```csharp
[MviBind(
    StateProperty = nameof(LoginState.UserName),
    BindingMode = MviBindingMode.TwoWay,
    IntentType = typeof(LoginIntent.ChangeUserName))]
public partial string UserName { get; set; }
```

生成器负责编译期生成完整实现。

这样可以减少大量样板代码，并避免运行时反射。

---

### 2.5 高性能 DI

DI 采用编译期生成方案，而不是运行时扫描。

设计目标：

- 不使用运行时反射创建对象。
- 不使用字符串查找。
- 编译期生成强类型工厂。
- 支持 MVI Store、ViewModel、ReducerDispatcher、EffectDispatcher、Mediator、ViewRegistry 的创建。
- 减少复杂业务系统中重复的依赖注册代码。

---

### 2.6 真正的 Mediator，而不是事件总线

框架中的 Mediator 不做发布订阅，不做事件中心。

它采用明确的 Request / Response 协调模型：

```text
子 MVI -> Mediator Request -> 父 MVI / 目标 MVI -> Response
```

这样可以避免复杂页面中出现大量事件订阅、广播和隐式依赖。

适合处理：

- 父界面协调子界面。
- 子界面向父界面提交数据。
- 一个子 MVI 的结果传递给另一个子 MVI。
- 可复用 MVI 模块与宿主页面解耦。
- 跨组件业务流程编排。

---

### 2.7 Middleware 中间件

中间件用于在 Intent 进入 Reducer 前处理横切逻辑。

典型用途：

- 表单校验。
- 权限检查。
- 防重复提交。
- 操作审计。
- 日志记录。
- 性能统计。
- 异常转换。
- 业务规则前置拦截。

中间件可以让业务流程保持清晰，不需要把校验、日志、审计全部写进 Reducer 或 ViewModel。

---

### 2.8 Analyzer 编译期约束

项目内置 Analyzer 目标是保证架构长期不腐化。

Analyzer 重点检查三类问题：

1. **Clean Architecture 分层引用**
   - 禁止 Domain 引用外层。
   - 禁止 Application 引用 Infrastructure / Presentation。
   - 禁止 src 引用 sample。

2. **微软 C# 编码规范**
   - 公共 API 命名。
   - 可访问性修饰符。
   - IDisposable 释放。
   - 参数判空。
   - Nullable 规范。

3. **中文 XML 注释**
   - 类、接口、方法、属性、字段、常量等公共成员必须有中文注释。

不把纯语法糖偏好一刀切设为错误，例如是否使用主构造函数、集合表达式等。

---

## 3. 解决的痛点

### 3.1 ViewModel 重复代码过多

传统 MVVM 中经常需要手写：

- backing field
- PropertyChanged
- Command
- CanExecute
- 状态同步
- setter 触发逻辑

本项目通过 Source Generator 自动生成这些代码，开发者只需要声明绑定关系。

---

### 3.2 状态变化不可追踪

传统事件回调容易导致状态散落在多个地方。

本项目通过 MVI 单向数据流统一状态变化入口：

```text
Intent -> Reducer -> State
```

所有状态变更都有来源，便于调试和测试。

---

### 3.3 复杂界面通信混乱

复杂 Dashboard、大型 HIS、MES、EMR 页面通常由多个子组件组成。

如果组件之间直接互相引用，很快会变成网状依赖。

本项目通过 Mediator 进行显式协调：

```text
子 MVI 不直接引用兄弟 MVI
子 MVI 不直接引用父页面实现
通信通过 Request / Response 完成
```

---

### 3.4 复用困难

很多业务界面都会重复出现：

- 患者检索
- 审计时间线
- 表单状态
- 流程状态
- 风险提醒
- 指标卡片

本项目将这些能力抽象为可复用 MVI Feature，可以被多个业务页面组合使用，而不是复制代码。

---

## 4. 如何使用

### 4.1 创建 State

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

---

### 4.2 创建 Intent

```csharp
public abstract partial record LoginIntent : IMviIntent
{
    public sealed partial record ChangeUserName(string UserName) : LoginIntent;

    public sealed partial record ChangePassword(string Password) : LoginIntent;

    public sealed partial record Submit : LoginIntent;
}
```

---

### 4.3 创建 Effect

```csharp
public abstract partial record LoginEffect : IMviEffect
{
    public sealed partial record RequestLogin(string UserName, string Password) : LoginEffect;
}
```

---

### 4.4 编写 Reducer

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

Reducer 只做状态转换，不访问 UI，不直接执行异步任务。

---

### 4.5 编写 ViewModel

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

生成器会自动生成属性、命令、状态映射和 Intent 派发逻辑。

---

### 4.6 编写 EffectDispatcher

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

副作用处理可以执行接口请求、导航、Mediator 协调等操作。

---

### 4.7 编写 View

Avalonia View 只负责绑定 ViewModel。

```xml
<TextBox Text="{Binding UserName, Mode=TwoWay}" />
<Button Command="{Binding SubmitCommand}" Content="登录" />
```

View 不写业务逻辑。

---

### 4.8 使用 Middleware

中间件用于处理通用逻辑。

```csharp
public sealed class LoginValidationMiddleware
    : IMviMiddleware<LoginState, LoginIntent, LoginEffect>
{
    public ValueTask<MviReduceResult<LoginState, LoginEffect>> InvokeAsync(
        MviMiddlewareContext<LoginState, LoginIntent, LoginEffect> context,
        MviMiddlewareStep<LoginState, LoginIntent, LoginEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        return nextMiddleware(context, cancellationToken);
    }
}
```

可用于校验、审计、权限、日志、性能统计等。

---

### 4.9 组件通信

通过 Mediator 传输数据，不直接引用其他组件。

```csharp
public sealed record PatientSelectedRequest(
    string SourceComponent,
    string PatientId,
    string PatientName);
```

```csharp
await mediator.SendAsync<PatientSelectedRequest, PatientSelectedResponse>(
    new PatientSelectedRequest("PatientSearch", "P001", "张三"),
    cancellationToken);
```

父页面或组合根负责协调目标子 MVI。

---

## 5. 本地运行

### 5.1 还原依赖

```bash
dotnet restore MiKiNuo.Mvi.slnx
```

### 5.2 编译

```bash
dotnet build MiKiNuo.Mvi.slnx
```

### 5.3 运行测试

```bash
dotnet test MiKiNuo.Mvi.slnx
```

### 5.4 启动 Avalonia 示例

```bash
dotnet run --project sample/MiKiNuo.Mvi.Samples.Avalonia/MiKiNuo.Mvi.Samples.Avalonia.csproj
```

