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
/// <summary>绑定资源清理不能因一项失败而提前中断。</summary>
public sealed class BindingLifetimeTests
{
    /// <summary>逆序释放所有资源并聚合失败。</summary>
    [Test] public async Task CleanupContinuesAfterFailureAsync()
    {
        List<string> calls = []; MviDisposableBag bag = new();
        bag.Add(() => calls.Add("A"));
        bag.Add(() => { calls.Add("B"); throw new InvalidOperationException("B"); });
        bag.Add(() => { calls.Add("C"); throw new InvalidOperationException("C"); });
        AggregateException error = await TestCheck.ThrowsAsync<AggregateException>(() => { bag.Dispose(); return Task.CompletedTask; });
        bag.Dispose();
        await Assert.That(string.Join(",", calls)).IsEqualTo("C,B,A");
        await Assert.That(error.InnerExceptions.Count).IsEqualTo(2);
    }
    /// <summary>已释放集合接收到资源时立即清理。</summary>
    [Test] public async Task LateResourceIsImmediatelyReleasedAsync()
    {
        MviDisposableBag bag = new(); bag.Dispose(); int released = 0;
        bag.Add(() => released++);
        await Assert.That(released).IsEqualTo(1);
    }
    /// <summary>原生事件解绑后不再派发，但不销毁 Store。</summary>
    [Test] public async Task EventSubscriptionFollowsBindingLifetimeAsync()
    {
        await using var store = TestCheck.Store(); using CounterViewModel model = new(store);
        Action<int>? emit = null;
        DelegateEventSource<int> source = new(handler => { emit = handler; return new ActionDisposable(() => emit = null); });
        using MviDisposableBag bag = new();
        bag.Add(new EventBinding<int>(source, static _ => new CounterIntent.Increment()).Attach(model.GetIntentDispatcher()));
        emit!(1); bag.Dispose(); emit?.Invoke(1);
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(store.CurrentState.Count).IsEqualTo(2);
    }
    /// <summary>成功登记不立即清理，集合释放和重复释放只执行动作一次。</summary>
    [Test] public async Task AddActionTransfersOwnershipUntilDisposalAsync()
    {
        using MviDisposableBag bag = new();
        int released = 0;
        bag.Add(() => released++);

        await Assert.That(released).IsEqualTo(0);
        bag.Dispose();
        bag.Dispose();
        await Assert.That(released).IsEqualTo(1);
    }
    /// <summary>关闭后登记的清理动作即使抛出异常，也不会被 finally 再执行一次。</summary>
    [Test] public async Task LateActionFailureDoesNotInvokeActionTwiceAsync()
    {
        using MviDisposableBag bag = new();
        bag.Dispose();
        int released = 0;
        InvalidOperationException failure = new("清理失败");

        InvalidOperationException error = await TestCheck.ThrowsAsync<InvalidOperationException>(() =>
        {
            bag.Add(() => { released++; throw failure; });
            return Task.CompletedTask;
        });

        bag.Dispose();
        await Assert.That(ReferenceEquals(error, failure)).IsTrue();
        await Assert.That(released).IsEqualTo(1);
    }
    /// <summary>空清理动作在创建资源之前被拒绝，并保留公开参数名。</summary>
    [Test] public async Task NullCleanupActionIsRejectedAsync()
    {
        using MviDisposableBag bag = new();
        ArgumentNullException error = await TestCheck.ThrowsAsync<ArgumentNullException>(() =>
        {
            bag.Add((Action)null!);
            return Task.CompletedTask;
        });

        await Assert.That(error.ParamName).IsEqualTo("disposeAction");
    }
}
