using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Presentation.Binding;
using MiKiNuo.Mvi.Presentation.Command;
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
        using MviMutationStore<EventCommandState, EventCommandIntent, EventCommandMutation, EventCommandEffect> store = new(
            EventCommandState.Initial,
            new EventCommandIntentHandler(),
            new EventCommandMutationReducer(),
            new EmptyEventCommandEffectDispatcher());
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
    /// 验证 <see cref="MviCommandBridge"/> 包装的 <c>ICommand</c> 适配器把
    /// <see cref="System.Windows.Input.ICommand.CanExecute(object?)"/>、
    /// <see cref="System.Windows.Input.ICommand.Execute(object?)"/> 与
    /// <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> 事件
    /// 正确转发到底层 <see cref="IMviCommand"/>。
    /// </summary>
    [Test]
    public async Task CommandBridge_Should_ForwardCanExecuteExecuteAndEventToMviCommandAsync()
    {
        int executedCount = 0;
        object? executedParameter = null;
        RecordingMviCommand mviCommand = new(
            canExecute: static _ => true,
            execute: parameter =>
            {
                executedCount++;
                executedParameter = parameter;
            });

        System.Windows.Input.ICommand adapter = MviCommandBridge.ToICommand(mviCommand);

        await Assert.That(adapter.CanExecute("payload")).IsTrue();
        adapter.Execute("payload");
        await Assert.That(executedCount).IsEqualTo(1);
        await Assert.That(executedParameter).IsEqualTo("payload");

        int changedCount = 0;
        EventHandler handler = (_, _) => changedCount++;
        adapter.CanExecuteChanged += handler;
        mviCommand.RaiseCanExecuteChanged();
        await Assert.That(changedCount).IsEqualTo(1);
        adapter.CanExecuteChanged -= handler;
        mviCommand.RaiseCanExecuteChanged();
        await Assert.That(changedCount).IsEqualTo(1);
    }

    /// <summary>
    /// 验证 <see cref="MviCommandBridge.Adapt"/> 与 <see cref="MviCommandBridge.Instance"/> 行为一致。
    /// </summary>
    [Test]
    public async Task CommandBridgeAdapt_Should_ReturnICommandImplAsync()
    {
        RecordingMviCommand mviCommand = new(static _ => true, static _ => { });
        System.Windows.Input.ICommand adapter = MviCommandBridge.Instance.Adapt(mviCommand);

        await Assert.That(adapter).IsNotNull();
        await Assert.That(adapter.CanExecute(null)).IsTrue();
    }

    /// <summary>
    /// 验证 <see cref="MviCommandBridge.ToICommand"/> 对 null 入参抛 <see cref="ArgumentNullException"/>。
    /// </summary>
    [Test]
    public void CommandBridge_Should_ThrowOnNullCommand()
    {
        Assert.Throws<ArgumentNullException>(() => MviCommandBridge.ToICommand(null!));
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

    private sealed class EmptyEventCommandEffectDispatcher
        : IMviEffectDispatcher<EventCommandEffect>
    {
        /// <summary>
        /// 分发副作用。
        /// </summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步分发过程的任务。</returns>
        public ValueTask DispatchAsync(EventCommandEffect effect, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 平台无关的 IMviCommand 测试替身，模拟命令的 CanExecute/Execute/CanExecuteChanged 行为。
    /// </summary>
    private sealed class RecordingMviCommand : IMviCommand
    {
        private readonly Predicate<object?> _canExecute;
        private readonly Action<object?> _execute;

        /// <summary>
        /// 初始化记录 MVI 命令。
        /// </summary>
        /// <param name="canExecute">可执行判断。</param>
        /// <param name="execute">执行动作。</param>
        public RecordingMviCommand(Predicate<object?> canExecute, Action<object?> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        /// <summary>
        /// 获取已执行的载荷集合。
        /// </summary>
        public List<object?> ExecutedPayloads { get; } = [];

        /// <summary>
        /// 判断命令是否可执行。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        /// <returns>可执行返回 true。</returns>
        public bool CanExecute(object? parameter) => _canExecute(parameter);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        public void Execute(object? parameter)
        {
            ExecutedPayloads.Add(parameter);
            _execute(parameter);
        }

        /// <summary>
        /// 当 CanExecute 结果可能变化时触发。
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 触发可执行状态变化。
        /// </summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
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
/// 表示事件命令测试变更。
/// </summary>
public abstract record EventCommandMutation : IMviMutation<EventCommandState>
{
    /// <summary>
    /// 表示设置文本的变更。
    /// </summary>
    /// <param name="Value">文本值。</param>
    [MviMutation(Path = "Text")]
    public sealed record SetText(string Value) : EventCommandMutation;

    /// <summary>
    /// 表示设置是否用户发起的变更。
    /// </summary>
    /// <param name="Value">是否用户发起。</param>
    [MviMutation(Path = "WasUserInitiated")]
    public sealed record SetWasUserInitiated(bool Value) : EventCommandMutation;
}

/// <summary>
/// 表示事件命令测试意图处理器。
/// </summary>
public sealed class EventCommandIntentHandler
    : IMviIntentHandler<EventCommandState, EventCommandIntent, EventCommandMutation, EventCommandEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<EventCommandMutation, EventCommandEffect>> HandleAsync(
        EventCommandState state,
        EventCommandIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<EventCommandMutation, EventCommandEffect> result = intent switch
        {
            EventCommandIntent.CaptureText captureText => MviHandleResult.Mutations<EventCommandMutation, EventCommandEffect>(
                new EventCommandMutation.SetText(captureText.Payload.Text),
                new EventCommandMutation.SetWasUserInitiated(captureText.Payload.IsUserInitiated)),
            _ => MviHandleResult.Empty<EventCommandMutation, EventCommandEffect>(),
        };
        return new ValueTask<MviHandleResult<EventCommandMutation, EventCommandEffect>>(result);
    }
}

/// <summary>
/// 表示事件命令测试变更规约器。
/// </summary>
public sealed partial class EventCommandMutationReducer
    : MviMutationReducerBase<EventCommandState, EventCommandMutation, EventCommandEffect>
{
    /// <summary>
    /// 应用设置文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventCommandState, EventCommandEffect> HandleSetText(
        EventCommandState state,
        EventCommandMutation.SetText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventCommandState, EventCommandEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置是否用户发起变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventCommandState, EventCommandEffect> HandleSetWasUserInitiated(
        EventCommandState state,
        EventCommandMutation.SetWasUserInitiated mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventCommandState, EventCommandEffect>(state.Apply(mutation));
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
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取捕获文本命令。
    /// </summary>
    [MviCommand(typeof(EventCommandIntent.CaptureText))]
    public partial IMviCommand CaptureTextCommand { get; private set; }
}
