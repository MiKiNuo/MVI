using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI 命令的回归测试。
/// </summary>
public sealed class MviCommandTests
{
    /// <summary>
    /// 验证 <see cref="MviCommand"/> 在 <c>canExecute</c> 变化时通过注入的
    /// <see cref="IMviUiDispatcher"/> 派发 <see cref="MviCommandBase.CanExecuteChanged"/>。
    /// 这是 <c>MviUiNotificationDispatcher</c> 删除后 UI 线程通知路径的唯一来源。
    /// </summary>
    [Test]
    public async Task MviCommand_Should_PostCanExecuteChangedToInjectedDispatcherAsync()
    {
        RecordingUiDispatcher dispatcher = new();
        using Subject<bool> canExecute = new();
        using MviCommand command = new(
            canExecute,
            static _ => { },
            dispatcher);

        int callCount = 0;
        command.CanExecuteChanged += (_, _) => Interlocked.Increment(ref callCount);

        canExecute.OnNext(true);

        await Assert.That(dispatcher.PostedCount).IsEqualTo(1);
        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(command.CanExecute(null)).IsTrue();
    }

    /// <summary>
    /// 验证 <c>canExecute</c> 状态未变时不会重复派发 <see cref="MviCommandBase.CanExecuteChanged"/>。
    /// 避免 Avalonia 命令绑定在每次 reducer 完成时刷一次 CanExecuteChanged 触发雪崩重绑定。
    /// </summary>
    [Test]
    public async Task MviCommand_Should_NotPostCanExecuteChanged_WhenValueUnchangedAsync()
    {
        RecordingUiDispatcher dispatcher = new();
        using Subject<bool> canExecute = new();
        using MviCommand command = new(
            canExecute,
            static _ => { },
            dispatcher);

        canExecute.OnNext(true);

        await Assert.That(dispatcher.PostedCount).IsEqualTo(1);
        await Assert.That(command.CanExecute(null)).IsTrue();
    }

    /// <summary>
    /// 验证 <see cref="MviAsyncCommand.Execute(object?)"/> 在异步执行抛出异常时
    /// 通过 <see cref="MviCommandBase.UnhandledException"/> 事件转发，而不是被静默吞掉。
    /// </summary>
    [Test]
    public async Task MviAsyncCommand_Should_ForwardExceptionThroughUnhandledExceptionEventAsync()
    {
        InvalidOperationException thrown = new("模拟业务异常");
        using MviAsyncCommand command = new(
            Observable.Return(true),
            CreateFailingExecuteAsync(thrown));

        CommandExceptionEventArgs? captured = null;
        command.UnhandledException += (_, args) => captured = args;

        command.Execute("payload");

        // 给 fire-and-forget 异步路径最多 200ms 完成，避免依赖 Thread.Sleep。
        for (int i = 0; i < 20 && captured is null; i++)
        {
            await Task.Delay(10);
        }

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Exception).IsSameReferenceAs(thrown);
        await Assert.That(captured.Parameter).IsEqualTo("payload");
    }

    /// <summary>
    /// 创建一个会异步抛出指定异常的 <see cref="Func{T1, T2, TResult}"/>，
    /// 让 <see cref="MviAsyncCommand"/> 走真正的未完成异步路径，便于触发 UnhandledException 事件。
    /// </summary>
    /// <param name="exception">待抛出异常。</param>
    /// <returns>异步抛异常的委托。</returns>
    private static Func<object?, CancellationToken, ValueTask> CreateFailingExecuteAsync(Exception exception)
    {
        return async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        };
    }
}

/// <summary>
/// 测试用的 <see cref="IMviUiDispatcher"/>：记录 <see cref="Post"/> 调用次数并同步执行回调。
/// </summary>
internal sealed class RecordingUiDispatcher : IMviUiDispatcher
{
    private int _postedCount;

    /// <summary>已派发的回调次数。</summary>
    public int PostedCount => Volatile.Read(ref _postedCount);

    /// <summary>
    /// 派发 UI 回调到调度器。
    /// </summary>
    /// <param name="action">回调动作。</param>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Interlocked.Increment(ref _postedCount);
        action();
    }
}
