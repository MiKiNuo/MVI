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
/// <summary>命令准入、错误观察和取消的实际行为。</summary>
public sealed class MviCommandTests
{
    /// <summary>void 入口观察同步构造异常且不保存敏感参数。</summary>
    [Test] public async Task SynchronousFailureIsReportedWithoutPayloadAsync()
    {
        InvalidOperationException expected = new("sync");
        using MviAsyncCommand command = new(static () => true, (_, _) => throw expected);
        CommandExceptionEventArgs? reported = null;
        command.UnhandledException += (_, error) => reported = error;
        command.Execute("secret");
        await Assert.That(reported!.Exception).IsSameReferenceAs(expected);
        await Assert.That(reported.Parameter).IsNull();
        await Assert.That(command.IsRunning).IsFalse();
    }
    /// <summary>异步失败通过同一个通道报告。</summary>
    [Test] public async Task AsynchronousFailureIsReportedAsync()
    {
        TaskCompletionSource release = TestCheck.Signal();
        TaskCompletionSource<Exception> reported = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using MviAsyncCommand command = new(static () => true, async (_, _) => { await release.Task; throw new InvalidOperationException("async"); });
        command.UnhandledException += (_, error) => reported.TrySetResult(error.Exception);
        command.Execute(null); release.TrySetResult();
        Exception exception = await reported.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(exception.Message).IsEqualTo("async");
    }
    /// <summary>按钮和原生事件重复触发同一个命令时只运行一次。</summary>
    [Test] public async Task RunningCommandRejectsDuplicateExecutionAsync()
    {
        TaskCompletionSource release = TestCheck.Signal(); int calls = 0;
        using MviAsyncCommand command = new(static () => true, async (_, _) => { calls++; await release.Task; });
        Task first = command.ExecuteAsync(null).AsTask();
        try
        {
            await command.ExecuteAsync(null); command.Execute(null);
            await Assert.That(calls).IsEqualTo(1);
            await Assert.That(command.CanExecute(null)).IsFalse();
        }
        finally { release.TrySetResult(); await first; }
        await Assert.That(command.CanExecute(null)).IsTrue();
    }
    /// <summary>显式异步调用把异常交还给调用方。</summary>
    [Test] public async Task AwaitedExecutionPreservesExceptionAsync()
    {
        using MviAsyncCommand command = new(static () => true, (_, _) => throw new InvalidOperationException("await"));
        await TestCheck.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync(null).AsTask());
    }
    /// <summary>释放后的命令不会执行新的任务。</summary>
    [Test] public async Task DisposedCommandCannotExecuteAsync()
    {
        int calls = 0;
        MviAsyncCommand command = new(static () => true, (_, _) => { calls++; return ValueTask.CompletedTask; });
        command.Dispose(); command.Execute(null); await command.ExecuteAsync(null);
        await Assert.That(calls).IsEqualTo(0);
    }
}
