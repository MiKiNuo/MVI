using Avalonia.Controls;
using Avalonia.Interactivity;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Presentation.Command;
using MiKiNuo.Mvi.Presentation.Events;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 ViewEvent 到 Command 绑定测试。
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
            new EventCommandReducer(),
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
    /// 验证 Debounce 只执行最后一次事件载荷。
    /// </summary>
    [Test]
    public async Task Debounce_Should_ExecuteLastPayloadOnlyAsync()
    {
        RecordingCommand command = new(static _ => true);
        using MviViewEventCommandBinding binding = new(
            command,
            new MviViewEventBindingOptions(Debounce: TimeSpan.FromMilliseconds(40)));

        binding.Handle(new MviActionEventPayload("SearchBox", "TextChanged", "first"));
        await Task.Delay(10);
        binding.Handle(new MviActionEventPayload("SearchBox", "TextChanged", "second"));
        await Task.Delay(80);

        await Assert.That(command.ExecutedPayloads.Count).IsEqualTo(1);
        await Assert.That(((MviActionEventPayload)command.ExecutedPayloads[0]!).RawEventArgs).IsEqualTo("second");
    }

    /// <summary>
    /// 验证绑定释放后会丢弃尚未触发的 Debounce 事件。
    /// </summary>
    [Test]
    public async Task Dispose_Should_DiscardPendingDebounceAsync()
    {
        RecordingCommand command = new(static _ => true);
        using MviViewEventCommandBinding binding = new(
            command,
            new MviViewEventBindingOptions(Debounce: TimeSpan.FromMilliseconds(40)));

        binding.Handle(new MviActionEventPayload("SearchBox", "TextChanged", "pending"));
        binding.Dispose();
        await Task.Delay(80);

        await Assert.That(command.ExecutedPayloads).IsEmpty();
    }

    /// <summary>
    /// 验证 CanExecute 为 false 时事件绑定不会执行命令。
    /// </summary>
    [Test]
    public async Task Handle_Should_NotExecuteCommand_WhenCanExecuteIsFalseAsync()
    {
        RecordingCommand command = new(static _ => false);
        using MviViewEventCommandBinding binding = new(command, MviViewEventBindingOptions.None);

        binding.Handle(new MviActionEventPayload("SubmitButton", "Pressed", null));

        await Assert.That(command.ExecutedPayloads).IsEmpty();
    }

    /// <summary>
    /// 验证 Avalonia Action attached behavior 会执行命令并传入动作载荷。
    /// </summary>
    [Test]
    public async Task AvaloniaActionCommand_Should_ExecuteCommandWithActionPayloadAsync()
    {
        Button button = new()
        {
            Name = "SubmitButton"
        };
        RecordingCommand command = new(static _ => true);

        ViewEvents.SetActionName(button, "Click");
        ViewEvents.SetActionCommand(button, command);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        await Assert.That(command.ExecutedPayloads.Count).IsEqualTo(1);
        MviActionEventPayload payload = (MviActionEventPayload)command.ExecutedPayloads[0]!;
        await Assert.That(payload.SourceName).IsEqualTo("SubmitButton");
        await Assert.That(payload.ActionName).IsEqualTo("Click");
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
    /// 验证平台事件适配器共享跨平台 ViewEvent 命令绑定运行时。
    /// </summary>
    [Test]
    public async Task PlatformEventAdapters_Should_UseSharedViewEventCommandBindingRuntimeAsync()
    {
        string root = FindRepositoryRoot();
        string avaloniaEvents = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Platforms.Avalonia",
            "Events",
            "ViewEvents.cs"));
        string godotView = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Platforms.Godot",
            "Binding",
            "GodotMviControlView.cs"));

        await Assert.That(avaloniaEvents).Contains("MviViewEventCommandBinding");
        await Assert.That(avaloniaEvents).Contains("DetachedFromVisualTree");
        await Assert.That(godotView).Contains("MviViewEventCommandBinding");
        await Assert.That(godotView).Contains("MviViewEventBindingOptions");
    }

    /// <summary>
    /// 验证 Avalonia MVI View 基类提供绑定生命周期注册入口。
    /// </summary>
    [Test]
    public async Task AvaloniaMviView_Should_ExposeBindingLifecycleHelpersAsync()
    {
        string root = FindRepositoryRoot();
        string viewBase = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "MiKiNuo.Mvi.Platforms.Avalonia",
            "Views",
            "MviAvaloniaView.cs"));

        await Assert.That(viewBase).Contains("protected void RegisterBinding");
        await Assert.That(viewBase).Contains("OnDetachedFromVisualTree");
    }

    /// <summary>
    /// 验证 <c>IMviCommand</c> 抽象作为 ViewEvent 命令绑定的契约（不依赖 <c>System.Windows.Input</c>）。
    /// </summary>
    [Test]
    public async Task MviViewEventCommandBinding_Should_BeConstructibleFromIMviCommandAsync()
    {
        RecordingMviCommand command = new(static _ => true, static _ => { });

        using MviViewEventCommandBinding binding = new(command, MviViewEventBindingOptions.None);
        binding.Handle(new MviActionEventPayload("SubmitButton", "Pressed", null));

        await Assert.That(command.ExecutedPayloads.Count).IsEqualTo(1);
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

    private sealed class EmptyEventCommandEffectDispatcher
        : IMviEffectDispatcher<EventCommandEffect>
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool CanExecute(object? parameter) => _canExecute(parameter);

        /// <inheritdoc />
        public void Execute(object? parameter)
        {
            ExecutedPayloads.Add(parameter);
            _execute(parameter);
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 触发可执行状态变化。
        /// </summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 平台无关的 IMviCommand 测试替身，保留旧的 CanExecute/Execute 委托风格便于 ViewEventCommandBindingTests 复用。
    /// </summary>
    private sealed class RecordingCommand : IMviCommand
    {
        private readonly Predicate<object?> _canExecute;

        /// <summary>
        /// 初始化记录命令。
        /// </summary>
        /// <param name="canExecute">可执行判断。</param>
        public RecordingCommand(Predicate<object?> canExecute)
        {
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 获取已执行的载荷集合。
        /// </summary>
        public List<object?> ExecutedPayloads { get; } = [];

        /// <inheritdoc />
        public bool CanExecute(object? parameter) => _canExecute(parameter);

        /// <inheritdoc />
        public void Execute(object? parameter) => ExecutedPayloads.Add(parameter);

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
/// 表示事件命令测试规约器。
/// </summary>
public sealed partial class EventCommandReducer
    : MviReducerBase<EventCommandState, EventCommandIntent, EventCommandEffect>
{
    /// <summary>
    /// 处理捕获文本事件意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<EventCommandState, EventCommandEffect> Reduce(
        EventCommandState state,
        EventCommandIntent.CaptureText intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        EventCommandState nextState = state with
        {
            Text = intent.Payload.Text,
            WasUserInitiated = intent.Payload.IsUserInitiated
        };

        return MviReduceResult.State<EventCommandState, EventCommandEffect>(nextState);
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
