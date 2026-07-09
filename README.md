# MiKiNuo.Mvi

> 面向复杂业务 UI 的 **响应式组合式 MVI 框架**。  
> 基于 **.NET 10 + R3 + Source Generator + Clean Architecture + Analyzer + 编译期 DI**，用于构建可追踪、可测试、可复用、可扩展的 Avalonia / WinForms / Godot / Unity 等多平台 UI 架构。

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![R3](https://img.shields.io/badge/R3-Reactive-00A6D6)
![Avalonia](https://img.shields.io/badge/Avalonia-12.x-7B2CBF)
![Source Generator](https://img.shields.io/badge/Source%20Generator-Enabled-orange)
![License](https://img.shields.io/badge/License-MIT-green)

---

## 为什么做这个项目？

在 HIS、EMR、LIS、MES、ERP、WMS、工业上位机、桌面管理系统、游戏 UI 等复杂客户端系统中，界面通常不是一个简单页面，而是由大量子模块组成：

```text
业务页面
├─ 表单录入区
├─ 查询筛选区
├─ 列表区
├─ 明细区
├─ 流程区
├─ 状态区
├─ 审计日志区
└─ 多个可复用业务组件
```

传统 MVVM 在中小型页面里很好用，但当页面足够复杂后，ViewModel 很容易膨胀成“上帝对象”：

```text
ViewModel 里写属性绑定
ViewModel 里写 Command
ViewModel 里写业务判断
ViewModel 里调接口
ViewModel 里做页面跳转
ViewModel 里处理子组件通信
ViewModel 里维护多个局部状态
```

最后会出现这些问题：

| 痛点           | 结果                                                         |
| -------------- | ------------------------------------------------------------ |
| ViewModel 过重 | 绑定逻辑、业务逻辑、副作用混在一起                           |
| 状态变化来源多 | 很难知道是谁修改了界面                                       |
| 组件互相引用   | 父子、兄弟模块依赖变成网状                                   |
| 事件中心滥用   | Publish / Subscribe 到处飞，调用链不可见                     |
| 代码重复       | PropertyChanged、Command、CanExecute、Reducer 分发、DI 注册反复手写 |
| 平台耦合       | 业务交互逻辑绑死在 Avalonia / WinForms / Unity / Godot       |
| 测试困难       | UI、异步、状态、副作用混在一起，不好单测                     |

`MiKiNuo.Mvi` 的目标是把这些问题拆开：

```text
View 只绑定
ViewModel 只暴露属性和命令
Intent 表达用户意图
Reducer 只做状态转换
Effect 描述副作用
EffectDispatcher 执行副作用
Middleware 处理横切逻辑
Mediator 协调父子和兄弟 MVI
Source Generator 消除重复代码
Analyzer 防止架构腐化
```

---

## MVI 是什么？

MVI 是 **Model-View-Intent** 的架构思想。  
在本项目中，它的核心是：

> **界面不能随便修改状态。所有变化都必须通过 Intent 进入 Store，再由 Reducer 生成新的 State。**

普通状态流：

```text
View
  -> ViewModel
  -> Intent
  -> Middleware
  -> Reducer
  -> State
  -> R3 State Stream
  -> ViewModel
  -> View
```

副作用流：

```text
Reducer
  -> Effect
  -> EffectDispatcher
  -> Service / Mediator / Navigation / Async Task
  -> New Intent
```

组件通信流：

```text
子 MVI
  -> Effect
  -> EffectDispatcher
  -> Mediator.SendAsync(...)
  -> 父 MVI 协调
  -> 目标子 MVI Dispatch Intent
```

这使得每一次界面变化都有明确来源：

```text
谁触发？      Intent
怎么变化？    Reducer
变成什么？    State
做了什么副作用？Effect
影响了谁？    Mediator
```

---

## 为什么不是传统 MVVM？

MVVM 的核心优势是绑定简单，但复杂业务 UI 不只是绑定，还包含状态流、业务流、组件流和副作用流。

### MVVM 常见流向

```text
View
  <-> ViewModel
        -> Service
        -> Event / Message
        -> Other ViewModel
```

问题是状态变化可以来自很多地方，数据流不够统一。

### MiKiNuo.Mvi 的流向

```text
View
  -> Intent
  -> Reducer
  -> State
  -> View
```

所有状态变化都通过一个方向流动。ViewModel 不再承载业务，只是 UI 绑定适配层。

| 对比点    | 传统 MVVM                  | MiKiNuo.Mvi                              |
| --------- | -------------------------- | ---------------------------------------- |
| ViewModel | 容易变重                   | 只暴露属性和命令                         |
| 状态变化  | 来源分散                   | Intent -> Reducer -> State               |
| 副作用    | 常写在 Command / ViewModel | Effect -> EffectDispatcher               |
| 组件通信  | 直接引用或事件总线         | Mediator Request / Response              |
| 代码重复  | 手写属性、命令、通知       | Source Generator 自动生成                |
| 测试      | 依赖 UI 和异步细节         | Reducer / Middleware / Effect 可独立测试 |
| 跨平台    | 容易绑定具体 UI 框架       | Core 不依赖具体平台                      |

---

## 架构亮点

### 1. R3 驱动响应式数据流

本项目不依赖 ReactiveUI，也不依赖 Rx.NET。核心响应式能力由 **R3** 提供。

R3 在项目中的作用：

| 场景               | R3 作用                                  |
| ------------------ | ---------------------------------------- |
| State Stream       | Store 推送最新状态                       |
| Effect Stream      | Store 推送副作用                         |
| Command CanExecute | 用 `Observable<bool>` 驱动命令可执行状态 |
| ViewModel 回写     | State Stream 驱动属性更新                |
| Middleware 诊断    | 记录 Intent、Reducer、Effect、耗时       |
| UI 通知调度        | 状态变化投递回 UI 线程                   |

View 对外仍然使用 UI 框架熟悉的绑定形式：

```xml
<TextBox Text="{Binding UserName, Mode=TwoWay}" />
<Button Command="{Binding SubmitCommand}" />
```

但内部不是 setter 直接修改业务，而是：

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

---

### 2. Source Generator 消除样板代码

传统 ViewModel 往往要写大量重复代码：

```text
backing field
PropertyChanged
Command
CanExecute
setter
状态同步
Intent 派发
Dispose
```

MiKiNuo.Mvi 用 Source Generator 自动生成这些代码。

你只需要声明绑定关系：

```csharp
[MviBind(
    StateProperty = nameof(LoginState.UserName),
    BindingMode = MviBindingMode.TwoWay,
    IntentType = typeof(LoginIntent.ChangeUserName))]
public partial string UserName { get; set; }
```

生成器负责生成：

```text
属性字段
PropertyChanged
双向绑定 setter
Intent 派发
ApplyStateCore
Command 初始化
CanExecute 订阅
Dispose 释放
```

生成器还负责生成：

```text
Reducer 分发入口
DI 容器工厂
ViewRegistry
组合根注册代码
```

业务代码不需要手写大段 switch，也不需要运行时反射扫描。

---

### 3. 实例化 Reducer，而不是静态工具类

早期 Reducer 很容易写成静态类：

```csharp
public static class LoginReducers
{
    public static MviReduceResult<LoginState, LoginEffect> Reduce(...)
}
```

这种方式简单，但不利于 DI、扩展和复杂 Feature 拆分。

当前设计推荐每个 MVI 拥有自己的 Reducer 对象：

```csharp
public sealed partial class LoginReducer
    : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> ReduceChangeUserName(
        LoginState state,
        LoginIntent.ChangeUserName intent)
    {
        LoginState nextState = state with
        {
            UserName = intent.UserName
        };

        return MviReduceResult.State(nextState);
    }
}
```

开发者只写具体 Intent 的处理方法。  
根 Intent 的分发入口由 Source Generator 生成。

---

### 4. 编译期 DI，不靠运行时扫描

项目中的 DI 方向是 **Source Generator 生成强类型工厂**。

目标：

```text
不使用运行时反射创建对象
不依赖字符串查找
不在启动时扫描程序集
编译期生成对象图
支持 Singleton / Scoped / Transient
支持 Store / ViewModel / Reducer / EffectDispatcher / Middleware / ViewRegistry
```

对于大型客户端项目，这可以减少启动开销，并让依赖错误更早暴露。

---

### 5. Mediator 是协调者，不是事件总线

本项目不鼓励事件中心式的发布订阅：

```text
Publish
Subscribe
Broadcast
```

而是使用明确的 Request / Response：

```csharp
await mediator.SendAsync<TRequest, TResponse>(request, cancellationToken);
```

适合处理：

```text
父 MVI 协调子 MVI
子 MVI 向父 MVI 提交数据
一个子 MVI 的结果传给另一个子 MVI
可复用 MVI 模块与宿主页面解耦
跨组件业务流程编排
```

---

### 6. Middleware 处理横切逻辑

中间件位于 Intent 进入 Reducer 之前：

```text
Intent
  -> Validation Middleware
  -> Logging Middleware
  -> Performance Middleware
  -> Reducer
```

适合放入：

```text
表单校验
权限检查
防重复提交
操作审计
日志记录
性能统计
异常转换
业务规则前置拦截
```

Reducer 保持纯粹，只做状态转换。

---

### 7. Clean Architecture + 平台拆分

当前仓库采用 `src / sample / test` 结构，并使用 `.slnx` 解决方案文件。

```text
src
├─ MiKiNuo.Mvi.Domain
├─ MiKiNuo.Mvi.Application
├─ MiKiNuo.Mvi.Infrastructure
├─ MiKiNuo.Mvi.Presentation
└─ MiKiNuo.Mvi.Platforms.Avalonia

sample
└─ MiKiNuo.Mvi.Samples.Avalonia

test
└─ MiKiNuo.Mvi.Tests
```

分层职责：

| 项目                             | 职责                                                         |
| -------------------------------- | ------------------------------------------------------------ |
| `MiKiNuo.Mvi.Domain`             | MVI / DI 基础抽象、标记接口、公共模型                        |
| `MiKiNuo.Mvi.Application`        | Store、Command、Reducer、Middleware、Mediator、ViewModel Base |
| `MiKiNuo.Mvi.Infrastructure`     | Source Generator、Analyzer、编译期工具                       |
| `MiKiNuo.Mvi.Presentation`       | 平台无关表现层抽象，例如 ViewRegistry、UI Dispatcher 抽象    |
| `MiKiNuo.Mvi.Platforms.Avalonia` | Avalonia 平台实现与 NuGet 入口包                             |

依赖方向：

```text
MiKiNuo.Mvi.Platforms.Avalonia
  -> MiKiNuo.Mvi.Presentation
  -> MiKiNuo.Mvi.Application
  -> MiKiNuo.Mvi.Domain
```

`MiKiNuo.Mvi.Infrastructure` 不作为运行时引用。  
源码开发时通过 `Directory.Build.targets` 作为 Analyzer / Source Generator 注入编译流程。

跨平台 View 自带事件绑定的设计决策见 [ViewEvent 到 Command 绑定设计决策](docs/view-event-command-binding.md)。

---

## 安装方式

> NuGet 包发布后，Avalonia 用户优先引用平台入口包。

```xml
<PackageReference Include="MiKiNuo.Mvi.Platforms.Avalonia" Version="x.y.z" />
```

该包会带入：

```text
MiKiNuo.Mvi.Domain
MiKiNuo.Mvi.Application
MiKiNuo.Mvi.Presentation
MiKiNuo.Mvi.Platforms.Avalonia
Source Generator / Analyzer
```

源码开发时，仓库通过 `Directory.Build.targets` 注入 `MiKiNuo.Mvi.Infrastructure`，无需 sample 项目直接引用所有底层项目。

---

## 快速开始

### 1. 创建 State

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

### 2. 创建 Intent

```csharp
public abstract partial record LoginIntent : IMviIntent
{
    public sealed partial record ChangeUserName(string UserName) : LoginIntent;

    public sealed partial record ChangePassword(string Password) : LoginIntent;

    public sealed partial record Submit : LoginIntent;
}
```

### 3. 创建 Effect

```csharp
public abstract partial record LoginEffect : IMviEffect
{
    public sealed partial record NavigateToDashboard(string DisplayName) : LoginEffect;
}
```

### 4. 编写实例化 Reducer

```csharp
public sealed partial class LoginReducer
    : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    [MviReduce(typeof(LoginIntent.ChangeUserName))]
    private MviReduceResult<LoginState, LoginEffect> HandleChangeUserName(
        LoginState state,
        LoginIntent.ChangeUserName intent,
        IMviBusinessResult? result)
    {
        LoginState nextState = state with
        {
            UserName = intent.UserName,
            ErrorMessage = null,
            CanSubmit = CanSubmit(intent.UserName, state.Password)
        };

        return Unchanged(nextState);
    }

    [MviReduce(typeof(LoginIntent.Submit), Guard = nameof(CanSubmitState))]
    private MviReduceResult<LoginState, LoginEffect> HandleSubmit(
        LoginState state,
        LoginIntent.Submit intent,
        IMviBusinessResult? result)
    {
        if (result is null)
        {
            return Unchanged(state with { IsBusy = true, ErrorMessage = null });
        }

        if (result is LoginBusinessResult.Success success)
        {
            LoginState nextState = state with
            {
                IsBusy = false,
                ErrorMessage = null,
                CanSubmit = true
            };

            return WithEffect(
                nextState,
                new LoginEffect.NavigateToDashboard(success.Profile.DisplayName));
        }

        if (result is LoginBusinessResult.Failure failure)
        {
            return Unchanged(state with
            {
                IsBusy = false,
                ErrorMessage = failure.ErrorMessage,
                CanSubmit = CanSubmit(state.UserName, state.Password)
            });
        }

        return Unchanged(state);
    }

    private bool CanSubmitState(LoginState state) => state.CanSubmit || state.IsBusy;

    private bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password);
    }
}
```

### 5. 编写 ViewModel

```csharp
public sealed partial class LoginViewModel
    : MviViewModelBase<LoginState, LoginIntent, LoginEffect>
{
    public LoginViewModel(IMviStore<LoginState, LoginIntent, LoginEffect> store)
        : base(store)
    {
    }

    [MviBind(
        nameof(LoginState.UserName),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(LoginIntent.ChangeUserName))]
    public partial string UserName { get; set; }

    [MviBind(
        nameof(LoginState.Password),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(LoginIntent.ChangePassword))]
    public partial string Password { get; set; }

    [MviBind(nameof(LoginState.CanSubmit))]
    public partial bool CanSubmit { get; private set; }

    [MviCommand(
        typeof(LoginIntent.Submit),
        CanExecuteProperty = nameof(CanSubmit),
        IsAsync = true)]
    public partial IMviAsyncCommand SubmitCommand { get; private set; }
}
```

### 6. 编写 EffectDispatcher

```csharp
public sealed class LoginEffectDispatcher : MviEffectDispatcherBase<LoginEffect>
{
    private readonly ILoginNavigationService _navigationService;

    public LoginEffectDispatcher(ILoginNavigationService navigationService)
    {
        _navigationService = navigationService
            ?? throw new ArgumentNullException(nameof(navigationService));
    }

    protected override async ValueTask DispatchCoreAsync(
        LoginEffect effect,
        CancellationToken cancellationToken)
    {
        if (effect is LoginEffect.NavigateToDashboard navigateToDashboard)
        {
            await _navigationService.NavigateToDashboardAsync(
                navigateToDashboard.DisplayName,
                cancellationToken);
        }
    }
}
```

### 7. View 只做绑定

```xml
<TextBox Text="{Binding UserName, Mode=TwoWay}" />
<TextBox Text="{Binding Password, Mode=TwoWay}" />
<Button Command="{Binding SubmitCommand}" Content="登录" />
```

---



---

## 本地运行

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

也可以使用脚本：

```bash
./build.sh
```

或 Windows PowerShell：

```powershell
./build.ps1
```

---
Avalonia 用户入口包：

```text
MiKiNuo.Mvi.Platforms.Avalonia
```

该包负责携带运行时平台适配和编译期 Analyzer / Source Generator。

---

## 设计原则

```text
View 只绑定
ViewModel 只暴露属性和命令
Reducer 只做状态转换
Effect 只描述副作用
EffectDispatcher 只执行副作用
Middleware 处理横切逻辑
Mediator 处理组件协调
Source Generator 生成重复代码
Analyzer 阻止架构腐化
Core 不依赖具体 UI 平台
```

---

## License

MIT
