using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证生成的 UI 命令复用操作入口与本地快照展示。</summary>
public sealed class OperationCommandTests
{
    /// <summary>禁用反馈不代替启动验证，命令等待不依赖展示。</summary>
    /// <returns>命令执行验证任务。</returns>
    [Test]
    public async Task CommandExecutesSameValidationAndDoesNotWaitForDisplay()
    {
        TaskCompletionSource<string> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectedOperationFeature feature = new(started, response.Task);
        ConcurrentQueue<Action> callbacks = new();
        using ProjectedOperationFeature.Projection projection = feature.CreateProjection(callbacks.Enqueue);
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsFalse();
        projection.SubmitAsyncCommand.Execute(null);
        OperationResult<int> rejected = await projection.SubmitAsyncCommand.Execution!;
        await Assert.That(rejected.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(started.Task.IsCompleted).IsFalse();
        feature.SetName("ready");
        projection.SubmitAsyncCommand.Execute(null);
        await Assert.That(await started.Task.WaitAsync(TimeSpan.FromSeconds(10))).IsEqualTo("ready");
        response.SetResult(7);
        OperationResult<int> completed = await projection.SubmitAsyncCommand.Execution!.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
        await Assert.That(projection.Snapshot.State.Result).IsEqualTo(0);
        while (callbacks.TryDequeue(out Action? callback))
        {
            callback();
        }

        await Assert.That(projection.Snapshot.State.Result).IsEqualTo(7);
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsTrue();
    }

    /// <summary>纯验证异常只禁用按钮反馈，实际调用返回故障，释放后不再执行。</summary>
    /// <returns>命令验证故障与释放验证任务。</returns>
    [Test]
    public async Task ValidationFaultAndDisposedProjectionAreHandled()
    {
        CommandRuleFeature feature = new();
        feature.SetName("fault");
        using CommandRuleFeature.Projection projection = feature.CreateProjection(static callback => callback());
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsFalse();
        projection.SubmitAsyncCommand.Execute(null);
        await Assert.That((await projection.SubmitAsyncCommand.Execution!).Kind).IsEqualTo(OperationResultKind.Faulted);
        int changed = 0;
        projection.SubmitAsyncCommand.CanExecuteChanged += (_, _) => changed++;
        feature.SetName("ready");
        await Assert.That(changed).IsEqualTo(1);
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsTrue();
        projection.Dispose();
        long before = feature.Snapshot.Version;
        Exception? failure = null;
        try { projection.SubmitAsyncCommand.Execute(null); }
        catch (Exception exception) { failure = exception; }
        await Assert.That(failure is ObjectDisposedException).IsTrue();
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsFalse();
        await Assert.That(feature.Snapshot.Version).IsEqualTo(before);
        feature.SetName("after-dispose");
        await Assert.That(changed).IsEqualTo(1);
    }
}

internal sealed record CommandRuleState
{
    [Input]
    public string Name { get; init; } = string.Empty;
}

internal sealed partial class CommandRuleFeature() : Feature<CommandRuleState>(new())
{
    [Operation(Validate = nameof(CanSubmit))]
    private ValueTask<int> SubmitAsync(Operation<CommandRuleState> operation) => ValueTask.FromResult(7);

    private static bool CanSubmit(CommandRuleState state) => state.Name == "fault"
        ? throw new InvalidOperationException("PRIVATE_INPUT") : state.Name.Length != 0;
}
