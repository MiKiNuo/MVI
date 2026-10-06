using System.Collections.Concurrent;
using MiKiNuo.Mvi.Abstractions.MVI.Binding;
using MiKiNuo.Mvi.Binding.Command;
using MiKiNuo.Mvi.Binding.Disposables;
using MiKiNuo.Mvi.Binding.EventBinding;
using MiKiNuo.Mvi.Binding.Threading;
using MiKiNuo.Mvi.Binding.ViewModel;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>测试 UI 调度所有权与声明式输入。</summary>
public sealed class ViewModelBindingTests
{
    /// <summary>后台状态提交不能立即改写 UI 拥有的属性缓冲。</summary>
    [Test] public async Task EntireProjectionIsQueuedOnUiDispatcherAsync()
    {
        await using var store = TestCheck.Store(); QueueUiDispatcher dispatcher = new();
        using CounterViewModel model = new(store, dispatcher); dispatcher.Drain();
        await Task.Run(async () => await store.DispatchAsync(new CounterIntent.Increment()));
        await Assert.That(model.Count).IsEqualTo(0);
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
        dispatcher.Drain();
        await Assert.That(model.Count).IsEqualTo(1);
        await Assert.That(model.State.Count).IsEqualTo(1);
    }
    /// <summary>释放后已经排队的状态回调失效。</summary>
    [Test] public async Task QueuedProjectionCannotMutateDisposedModelAsync()
    {
        await using var store = TestCheck.Store(); QueueUiDispatcher dispatcher = new();
        CounterViewModel model = new(store, dispatcher); dispatcher.Drain();
        await store.DispatchAsync(new CounterIntent.Increment());
        model.Dispose(); dispatcher.Drain();
        await Assert.That(model.Count).IsEqualTo(0);
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(store.CurrentState.Count).IsEqualTo(2);
    }
    /// <summary>一轮通知读取到的是同一完整状态投影。</summary>
    [Test] public async Task NotificationSeesCompleteProjectionAsync()
    {
        await using var store = TestCheck.Store();
        using CounterViewModel model = new(store);
        bool consistent = true;
        model.PropertyChanged += (_, _) => consistent &= model.Count == model.State.Count && model.Label == model.State.Label;
        await store.DispatchAsync(new CounterIntent.Increment());
        await store.DispatchAsync(new CounterIntent.Rename("x"));
        await Assert.That(consistent).IsTrue();
    }
    /// <summary>本地输入不会被业务状态更新覆盖。</summary>
    [Test] public async Task LocalDraftSurvivesUnrelatedStateProjectionAsync()
    {
        await using var store = TestCheck.Store(); using CounterViewModel model = new(store);
        model.Draft = "new input";
        await store.DispatchAsync(new CounterIntent.Rename("server state"));
        await Assert.That(model.Draft).IsEqualTo("new input");
        await model.RenameCommand.ExecuteAsync(null);
        await Assert.That(store.CurrentState.Label).IsEqualTo("new input");
    }
}
/// <summary>真实生成的属性与命令，不使用模拟基类。</summary>
public sealed partial class CounterViewModel : MviViewModelBase<CounterState, CounterIntent>
{
    /// <summary>获取状态计数。</summary>
    [MviBind] public partial int Count { get; }
    /// <summary>获取状态名称。</summary>
    [MviBind] public partial string Label { get; }
    /// <summary>获取或设置未提交输入。</summary>
    [MviBind] public partial string Draft { get; set; }
    /// <summary>提交本地输入的快照。</summary>
    [MviCommand(typeof(CounterIntent.Rename), nameof(Draft))]
    public partial IMviAsyncCommand RenameCommand { get; }
}
/// <summary>只由测试明确排空的确定性 UI 调度器。</summary>
internal sealed class QueueUiDispatcher : IMviUiDispatcher
{
    private readonly ConcurrentQueue<Action> _queue = new();
    /// <summary>排队但不立即执行。</summary>
    public void Post(Action action) => _queue.Enqueue(action);
    /// <summary>在测试指定的线程完整执行回调。</summary>
    internal void Drain() { while (_queue.TryDequeue(out Action? action)) action(); }
}
/// <summary>原有事件和命令测试共用的同步调度探针。</summary>
internal sealed class RecordingUiDispatcher : IMviUiDispatcher
{
    private int _count;
    /// <summary>获取派发次数。</summary>
    public int PostedCount => _count;
    /// <summary>记录后执行。</summary>
    public void Post(Action action) { _count++; action(); }
}
