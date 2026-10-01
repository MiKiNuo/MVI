using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证操作快照接入本地投影后仍保持提交与展示的完成边界。</summary>
public sealed class OperationProjectionTests
{
    /// <summary>验证启动展示调度失败仍返回已接纳任务和一致的结构化结果。</summary>
    /// <returns>表示副作用调度顺序验证结束的任务。</returns>
    [Test]
    public async Task StartDisplayFailureDoesNotStrandAcceptedEffect()
    {
        TaskCompletionSource<string> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectedOperationFeature feature = new(started, response.Task);
        feature.SetName("start");
        int schedules = 0;
        using OperationProbeProjection projection = new(feature, callback =>
        {
            if (Interlocked.Increment(ref schedules) == 1)
            {
                throw new InvalidOperationException("平台启动展示调度失败。");
            }

            callback();
        }, ProjectionMode.Coalesce);
        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await Assert.That(await started.Task.WaitAsync(TimeSpan.FromSeconds(10))).IsEqualTo("start");
        response.SetResult(7);
        OperationResult<int> result = await execution.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(result.Value).IsEqualTo(7);
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
        await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].LastResult).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].LastAttemptId).IsEqualTo(result.OperationId);
        await Assert.That(projection.Snapshot.State.Result).IsEqualTo(7);
    }

    /// <summary>验证反馈和完成展示失败独立诊断且不改变已提交的操作结果。</summary>
    /// <param name="failAtCompletion">是否在完成展示而非业务反馈展示时抛异常。</param>
    /// <returns>表示展示错误与操作结果隔离验证结束的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task DisplayFailuresAreDiagnosedWithoutChangingCommittedOperationResult(bool failAtCompletion)
    {
        const string SensitiveState = "PRIVATE_STATE_PAYLOAD";
        const string SensitiveMessage = "PRIVATE_EXCEPTION_MESSAGE";
        TaskCompletionSource<string> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectedOperationFeature feature = new(started, response.Task);
        feature.SetName(SensitiveState);
        long failedVersion = failAtCompletion ? 4L : 3L;
        using OperationDiagnosticListener diagnostics = new();
        Trace.Listeners.Add(diagnostics);
        try
        {
            using OperationProbeProjection projection = new(feature, callback =>
            {
                if (feature.Snapshot.Version == failedVersion)
                {
                    throw new InvalidOperationException(SensitiveMessage);
                }

                callback();
            }, ProjectionMode.Coalesce);
            Task<OperationResult<int>> execution = feature.SubmitAsync();
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            response.SetResult(7);
            OperationResult<int> result = await execution.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(result.Value).IsEqualTo(7);
            await Assert.That(result.Exception).IsNull();
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
            await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].LastResult).IsEqualTo(result.Kind);
            await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].LastAttemptId).IsEqualTo(result.OperationId);
            await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].IsRunning).IsFalse();
            string output = diagnostics.Output;
            await Assert.That(output.Contains(typeof(InvalidOperationException).FullName!, StringComparison.Ordinal)).IsTrue();
            await Assert.That(output.Contains("SnapshotVersion=" + failedVersion, StringComparison.Ordinal)).IsTrue();
            await Assert.That(output.Contains("OperationId=" + result.OperationId, StringComparison.Ordinal)).IsTrue();
            await Assert.That(output.Contains(SensitiveState, StringComparison.Ordinal)).IsFalse();
            await Assert.That(output.Contains(SensitiveMessage, StringComparison.Ordinal)).IsFalse();
            await Assert.That(output.Contains(nameof(DisplayFailuresAreDiagnosedWithoutChangingCommittedOperationResult), StringComparison.Ordinal)).IsFalse();
        }
        finally
        {
            Trace.Listeners.Remove(diagnostics);
        }
    }

    /// <summary>验证宿主诊断回调故障也不能破坏操作各阶段的结构化完成结果。</summary>
    /// <param name="failedVersion">启动、反馈或完成展示发生故障的提交版本。</param>
    /// <returns>表示诊断回调隔离验证结束的任务。</returns>
    [Test]
    [Arguments(2L)]
    [Arguments(3L)]
    [Arguments(4L)]
    public async Task ThrowingDiagnosticListenerCannotChangeOperationResult(long failedVersion)
    {
        TaskCompletionSource<string> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectedOperationFeature feature = new(started, response.Task);
        feature.SetName("start");
        using ThrowingOperationDiagnosticListener diagnostics = new();
        Trace.Listeners.Add(diagnostics);
        try
        {
            using OperationProbeProjection projection = new(feature, callback =>
            {
                if (feature.Snapshot.Version == failedVersion)
                {
                    throw new InvalidOperationException("平台展示故障。");
                }

                callback();
            }, ProjectionMode.Coalesce);
            Task<OperationResult<int>> execution = feature.SubmitAsync();
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            response.SetResult(7);
            OperationResult<int> result = await execution.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(result.Value).IsEqualTo(7);
            await Assert.That(result.Exception).IsNull();
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
            await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].LastResult).IsEqualTo(result.Kind);
            await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].LastAttemptId).IsEqualTo(result.OperationId);
            await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].IsRunning).IsFalse();
        }
        finally
        {
            Trace.Listeners.Remove(diagnostics);
        }
    }

    /// <summary>验证操作完成不等待展示，且每次运行事实都能按版本投影。</summary>
    /// <returns>表示操作与展示完成边界验证结束的任务。</returns>
    [Test]
    public async Task CompletionDoesNotWaitForDisplayAndEveryCommitKeepsOperationFacts()
    {
        TaskCompletionSource<string> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectedOperationFeature feature = new(started, response.Task);
        feature.SetName("start");
        ConcurrentQueue<Action> callbacks = new();
        using OperationProbeProjection projection = new(feature, callbacks.Enqueue, ProjectionMode.EveryCommit);

        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await Assert.That(await started.Task.WaitAsync(TimeSpan.FromSeconds(10))).IsEqualTo("start");
        await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].IsRunning).IsTrue();
        await Assert.That(projection.Snapshot.Version).IsEqualTo(1L);
        Drain(callbacks);
        await Assert.That(projection.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].IsRunning).IsTrue();

        feature.SetName("edited");
        response.SetResult(7);
        OperationResult<int> result = await execution.WaitAsync(TimeSpan.FromSeconds(10));

        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(result.Value).IsEqualTo(7);
        await Assert.That(feature.Snapshot.State.Name).IsEqualTo("edited");
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
        await Assert.That(feature.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].IsRunning).IsFalse();
        await Assert.That(projection.Snapshot.Version).IsEqualTo(2L);

        Drain(callbacks);
        await Assert.That(projection.Commits.Select(static snapshot => snapshot.Version).SequenceEqual([2L, 3L, 4L, 5L])).IsTrue();
        await Assert.That(ReferenceEquals(projection.Snapshot, feature.Snapshot)).IsTrue();
    }

    /// <summary>验证内联展示回调可以让另一个线程提交输入而不被 Store 门阻塞。</summary>
    /// <returns>表示展示回调门外执行验证结束的任务。</returns>
    [Test]
    public async Task InlineDisplayCanCommitConcurrentInputOutsideStoreGate()
    {
        TaskCompletionSource<string> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectedOperationFeature feature = new(started, response.Task);
        feature.SetName("start");
        using OperationProbeProjection projection = new(feature, static callback => callback(), ProjectionMode.Coalesce);
        int edited = 0;
        projection.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(projection.Snapshot) && Interlocked.Exchange(ref edited, 1) == 0)
            {
                Task writer = Task.Run(() => feature.SetName("from display"));
                writer.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
        };

        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await Assert.That(await started.Task.WaitAsync(TimeSpan.FromSeconds(10))).IsEqualTo("start");
        response.SetResult(7);
        OperationResult<int> result = await execution.WaitAsync(TimeSpan.FromSeconds(10));

        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(feature.Snapshot.State.Name).IsEqualTo("from display");
        await Assert.That(projection.Snapshot.State.Name).IsEqualTo("from display");
        await Assert.That(projection.Snapshot.OperationStates[nameof(ProjectedOperationFeature.SubmitAsync)].IsRunning).IsFalse();
    }

    private static void Drain(ConcurrentQueue<Action> callbacks)
    {
        while (callbacks.TryDequeue(out Action? callback))
        {
            callback();
        }
    }

    private sealed class ThrowingOperationDiagnosticListener : TraceListener
    {
        public override void Write(string? message) => throw new InvalidOperationException("诊断回调故障。");

        public override void WriteLine(string? message) => throw new InvalidOperationException("诊断回调故障。");
    }

    private sealed class OperationDiagnosticListener : TraceListener
    {
        private readonly ConcurrentQueue<string> messages = new();

        internal string Output => string.Join(Environment.NewLine, messages);

        public override void Write(string? message) => messages.Enqueue(message ?? string.Empty);

        public override void WriteLine(string? message) => messages.Enqueue(message ?? string.Empty);
    }
}

internal sealed record ProjectedOperationState
{
    [Input]
    public string Name { get; init; } = string.Empty;

    public int Result { get; init; }
}

internal sealed partial class ProjectedOperationFeature(TaskCompletionSource<string> started, Task<int> response)
    : Feature<ProjectedOperationState>(new())
{
    [Operation(Validate = nameof(CanSubmit))]
    private async ValueTask<int> SubmitAsync(Operation<ProjectedOperationState> operation)
    {
        started.SetResult(operation.Snapshot.Name);
        int result = await response.WaitAsync(operation.CancellationToken);
        await operation.UpdateAsync(ApplyResult, result);
        return result;
    }

    private static bool CanSubmit(ProjectedOperationState state) => !string.IsNullOrWhiteSpace(state.Name);

    private static ProjectedOperationState ApplyResult(ProjectedOperationState state, int result) => state with { Result = result };
}

internal sealed class OperationProbeProjection : FeatureProjection<ProjectedOperationState>
{
    internal OperationProbeProjection(ProjectedOperationFeature feature, Action<Action> schedule, ProjectionMode mode)
        : base(feature, schedule, mode)
    {
        InitializeProjection();
    }

    internal List<RuntimeSnapshot<ProjectedOperationState>> Commits { get; } = [];

    protected override void OnSnapshotChanged(ProjectedOperationState previous, ProjectedOperationState current)
        => Commits.Add(Snapshot);
}
