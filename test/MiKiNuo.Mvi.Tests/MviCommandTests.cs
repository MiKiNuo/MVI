using MiKiNuo.Mvi.Application.MVI.Command;
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
