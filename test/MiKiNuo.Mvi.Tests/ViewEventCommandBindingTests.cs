using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Presentation.Binding;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Presentation.Events;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示事件绑定适配器模式测试，验证 <see cref="IEventSource{TEvent}"/> / <see cref="EventBinding{TEvent}"/> / <see cref="MviComponent"/> 的核心行为。
/// </summary>
public sealed class ViewEventCommandBindingTests
{
    /// <summary>
    /// 验证命令收到事件 payload 后，会构造 payload Intent 并派发到 Store。
    /// </summary>
    [Test]
    public async Task PayloadCommand_Should_DispatchIntentConstructedFromPayloadAsync()
    {
        using MviStore<EventCommandState, EventCommandIntent, EventCommandEffect> store = new(
            EventCommandState.Initial,
            new EventCommandIntentHandler(),
            new EventCommandReducer(),
            new NoopEffectDispatcher<EventCommandEffect>());
        using EventCommandViewModel viewModel = new(store);

        viewModel.CaptureTextCommand.Execute(new MviTextChangedEventPayload(
            Text: "admin",
            PreviousText: string.Empty,
            IsUserInitiated: true,
            RawEventArgs: null));
        await Task.Delay(50);

        await Assert.That(store.CurrentState.Text).IsEqualTo("admin");
        await Assert.That(store.CurrentState.WasUserInitiated).IsTrue();
    }

    /// <summary>
    /// 验证 <see cref="DelegateEventSource{TEvent}"/> 订阅后触发事件会调用处理器，取消订阅后不再调用。
    /// </summary>
    [Test]
    public async Task DelegateEventSource_Should_InvokeHandlerAndUnsubscribeOnDisposeAsync()
    {
        TestEventSource rawSource = new();
        List<string> received = [];

        IEventSource<string> source = new DelegateEventSource<string>(handler =>
        {
            rawSource.Event += handler;
            return new ActionDisposable(() => rawSource.Event -= handler);
        });

        IDisposable subscription = source.Subscribe(e => received.Add(e));
        rawSource.Raise("first");
        rawSource.Raise("second");

        await Assert.That(received.Count).IsEqualTo(2);
        await Assert.That(received[0]).IsEqualTo("first");
        await Assert.That(received[1]).IsEqualTo("second");

        subscription.Dispose();
        rawSource.Raise("ignored");
        await Assert.That(received.Count).IsEqualTo(2);
    }

    /// <summary>
    /// 验证 <see cref="EventBinding{TEvent}"/> 在事件触发时派发映射后的 Intent，取消附加后不再派发。
    /// </summary>
    [Test]
    public async Task EventBinding_Should_DispatchMappedIntentOnEventAsync()
    {
        TestEventSource rawSource = new();
        List<IMviIntent> dispatched = [];

        IEventSource<string> eventSource = new DelegateEventSource<string>(handler =>
        {
            rawSource.Event += handler;
            return new ActionDisposable(() => rawSource.Event -= handler);
        });

        EventBinding<string> binding = new(eventSource, text => new TestIntent(text));
        IDisposable attachment = binding.Attach(intent => dispatched.Add(intent));

        rawSource.Raise("hello");

        await Assert.That(dispatched.Count).IsEqualTo(1);
        await Assert.That(((TestIntent)dispatched[0]!).Value).IsEqualTo("hello");

        attachment.Dispose();
        rawSource.Raise("ignored");
        await Assert.That(dispatched.Count).IsEqualTo(1);
    }

    /// <summary>
    /// 验证 <see cref="ActionDisposable"/> 只执行一次清理动作。
    /// </summary>
    [Test]
    public async Task ActionDisposable_Should_ExecuteActionOnlyOnceAsync()
    {
        int callCount = 0;
        ActionDisposable disposable = new(() => callCount++);

        disposable.Dispose();
        disposable.Dispose();

        await Assert.That(callCount).IsEqualTo(1);
    }

    /// <summary>
    /// 验证 <see cref="MviComponent.GetIntentDispatcher"/> 返回的派发器能把 Intent 转发到 protected Dispatch。
    /// </summary>
    [Test]
    public async Task MviComponent_GetIntentDispatcher_Should_ForwardIntentToDispatchAsync()
    {
        using TestComponent component = new();
        IMviIntentDispatcher dispatcher = component.GetIntentDispatcher();

        dispatcher.Dispatch(new TestIntent("hello"));
        await Assert.That(component.Dispatched.Count).IsEqualTo(1);
        await Assert.That(((TestIntent)component.Dispatched[0]!).Value).IsEqualTo("hello");
    }

    /// <summary>
    /// 验证 <see cref="EventBindingExtensions.BindTo{TEvent}"/> 把事件订阅注册到 <see cref="MviDisposableBag"/>，
    /// 释放 <see cref="MviDisposableBag"/> 后事件不再派发。
    /// </summary>
    [Test]
    public async Task BindTo_Should_RegisterSubscriptionToDisposableBagAndStopOnDisposeAsync()
    {
        TestEventSource rawSource1 = new();
        TestEventSource rawSource2 = new();
        using TestComponent component = new();
        IMviIntentDispatcher dispatcher = component.GetIntentDispatcher();
        MviDisposableBag bindings = new();

        IEventSource<string> source1 = new DelegateEventSource<string>(handler =>
        {
            rawSource1.Event += handler;
            return new ActionDisposable(() => rawSource1.Event -= handler);
        });
        IEventSource<string> source2 = new DelegateEventSource<string>(handler =>
        {
            rawSource2.Event += handler;
            return new ActionDisposable(() => rawSource2.Event -= handler);
        });

        source1.BindTo(dispatcher, text => new TestIntent(text), bindings);
        source2.BindTo(dispatcher, text => new TestIntent(text), bindings);

        rawSource1.Raise("a");
        rawSource2.Raise("b");
        await Assert.That(component.Dispatched.Count).IsEqualTo(2);

        bindings.Dispose();
        rawSource1.Raise("ignored1");
        rawSource2.Raise("ignored2");
        await Assert.That(component.Dispatched.Count).IsEqualTo(2);
    }

    /// <summary>
    /// 验证 <see cref="MviDisposableBag"/> 多次调用 Dispose 不会抛异常且保持幂等。
    /// </summary>
    [Test]
    public async Task DisposableBag_Should_BeIdempotentOnMultipleDisposeAsync()
    {
        TestEventSource rawSource = new();
        using TestComponent component = new();
        IMviIntentDispatcher dispatcher = component.GetIntentDispatcher();
        MviDisposableBag bindings = new();

        IEventSource<string> source = new DelegateEventSource<string>(handler =>
        {
            rawSource.Event += handler;
            return new ActionDisposable(() => rawSource.Event -= handler);
        });
        source.BindTo(dispatcher, text => new TestIntent(text), bindings);

        rawSource.Raise("before");
        await Assert.That(component.Dispatched.Count).IsEqualTo(1);

        bindings.Dispose();
        bindings.Dispose();

        rawSource.Raise("after");
        await Assert.That(component.Dispatched.Count).IsEqualTo(1);
    }

    /// <summary>
    /// 验证 Avalonia MVI View 基类提供 OnBind 钩子与可视树脱离清理。
    /// </summary>
    [Test]
    public async Task AvaloniaMviView_Should_ExposeOnBindHookAndDetachedCleanupAsync()
    {
        string root = FindRepositoryRoot();
        string viewBase = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Platforms.Avalonia",
            "Views",
            "MviAvaloniaView.cs"));

        await Assert.That(viewBase).Contains("protected virtual void OnBind(TViewModel viewModel, MviDisposableBag bindings)");
        await Assert.That(viewBase).Contains("OnDetachedFromVisualTree");
    }

    /// <summary>
    /// 验证 Godot MVI View 基类的 BindEvent/BindButton 不再依赖已删除的 MviViewEventCommandBinding 运行时。
    /// </summary>
    [Test]
    public async Task GodotMviView_Should_NotDependOnViewEventCommandBindingRuntimeAsync()
    {
        string root = FindRepositoryRoot();
        string godotView = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Platforms.Godot",
            "Binding",
            "GodotMviControlView.cs"));

        await Assert.That(godotView).Contains("BindEvent");
        await Assert.That(godotView).Contains("BindButton");
        await Assert.That(godotView).DoesNotContain("MviViewEventCommandBinding");
        await Assert.That(godotView).DoesNotContain("MviViewEventBindingOptions");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MiKiNuo.Mvi.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("未找到解决方案根目录。");
        }

        return directory.FullName;
    }

    /// <summary>
    /// 表示测试用事件源，模拟原生事件的订阅与触发。
    /// </summary>
    private sealed class TestEventSource
    {
        /// <summary>
        /// 原生事件。
        /// </summary>
        public event Action<string>? Event;

        /// <summary>
        /// 触发事件。
        /// </summary>
        /// <param name="e">事件数据。</param>
        public void Raise(string e) => Event?.Invoke(e);
    }

    /// <summary>
    /// 表示测试用意图。
    /// </summary>
    /// <param name="Value">意图值。</param>
    private sealed record TestIntent(string Value) : IMviIntent;

    /// <summary>
    /// 表示测试用 MVI 组件，记录派发的 Intent。
    /// </summary>
    private sealed class TestComponent : MviComponent
    {
        /// <summary>
        /// 获取已派发的意图集合。
        /// </summary>
        public List<IMviIntent> Dispatched { get; } = [];

        /// <summary>
        /// 派发 Intent 到 Store。
        /// </summary>
        /// <param name="intent">意图。</param>
        protected override void Dispatch(IMviIntent intent) => Dispatched.Add(intent);
    }
}

/// <summary>
/// 表示事件命令测试状态。
/// </summary>
/// <param name="Text">当前文本。</param>
/// <param name="WasUserInitiated">是否由用户输入触发。</param>
public sealed partial record EventCommandState(string Text, bool WasUserInitiated) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static EventCommandState Initial { get; } = new(string.Empty, false);
}

/// <summary>
/// 表示事件命令测试意图。
/// </summary>
public abstract partial record EventCommandIntent : IMviIntent
{
    /// <summary>
    /// 表示捕获文本事件意图。
    /// </summary>
    /// <param name="Payload">文本变化事件载荷。</param>
    public sealed partial record CaptureText(MviTextChangedEventPayload Payload) : EventCommandIntent;
}

/// <summary>
/// 表示事件命令测试副作用。
/// </summary>
public abstract partial record EventCommandEffect : IMviEffect;

/// <summary>
/// 表示事件命令测试意图处理器。
/// </summary>
public sealed class EventCommandIntentHandler
    : MviIntentHandlerBase<EventCommandState, EventCommandIntent, EventCommandEffect>
{
    /// <summary>
    /// 处理具体业务逻辑。
    /// </summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    protected override async ValueTask<IMviBusinessResult?> HandleCoreAsync(
        EventCommandState state,
        EventCommandIntent intent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return null;
    }
}

/// <summary>
/// 表示事件命令测试规约器。
/// </summary>
public sealed partial class EventCommandReducer
    : MviReducerBase<EventCommandState, EventCommandIntent, EventCommandEffect>
{
    /// <summary>
    /// 处理捕获文本意图产生新状态。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">捕获文本意图。</param>
    /// <param name="result">业务结果。</param>
    /// <returns>规约结果。</returns>
    [MviReduce(typeof(EventCommandIntent.CaptureText))]
    private MviReduceResult<EventCommandState, EventCommandEffect> HandleCaptureText(
        EventCommandState state,
        EventCommandIntent.CaptureText intent,
        IMviBusinessResult? result)
    {
        return MviReduceResult.State<EventCommandState, EventCommandEffect>(
            state with { Text = intent.Payload.Text, WasUserInitiated = intent.Payload.IsUserInitiated });
    }
}

/// <summary>
/// 表示事件命令测试 ViewModel。
/// </summary>
public sealed partial class EventCommandViewModel
    : MviViewModelBase<EventCommandState, EventCommandIntent, EventCommandEffect>
{
    /// <summary>
    /// 初始化事件命令测试 ViewModel。
    /// </summary>
    /// <param name="store">事件命令测试状态存储。</param>
    public EventCommandViewModel(IMviStore<EventCommandState, EventCommandIntent, EventCommandEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取捕获文本命令。
    /// </summary>
    [MviCommand(typeof(EventCommandIntent.CaptureText))]
    public partial IMviCommand CaptureTextCommand { get; private set; }
}
