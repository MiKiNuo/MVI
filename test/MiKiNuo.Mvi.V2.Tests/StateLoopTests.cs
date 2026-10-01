using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证生成输入通过快照完成独立实例状态闭环。</summary>
public sealed class StateLoopTests
{
    /// <summary>验证规则故障和空结果均保留原快照与版本。</summary>
    /// <returns>表示故障原子性验证完成的任务。</returns>
    [Test]
    public async Task RuleFailureAndNullResultKeepExactSnapshotAndVersion()
    {
        EditorFeature feature = new();
        feature.SetDelta(7);
        RuntimeSnapshot<EditorState> committed = feature.Snapshot;
        Exception? thrown = null;
        try
        {
            feature.SetDelta(-1);
        }
        catch (InvalidOperationException exception)
        {
            thrown = exception;
        }

        await Assert.That(thrown).IsNotNull();
        await Assert.That(ReferenceEquals(feature.Snapshot, committed)).IsTrue();
        thrown = null;
        try
        {
            feature.SetDelta(-2);
        }
        catch (ArgumentNullException exception)
        {
            thrown = exception;
        }

        await Assert.That(thrown).IsNotNull();
        await Assert.That(ReferenceEquals(feature.Snapshot, committed)).IsTrue();
        feature.SetDelta(1);
        await Assert.That(feature.Snapshot.State.Total).IsEqualTo(8);
        await Assert.That(feature.Snapshot.Version).IsEqualTo(2L);
        await Assert.That(committed.State.Total).IsEqualTo(7);
        await Assert.That(committed.Version).IsEqualTo(1L);
    }

    /// <summary>验证并发输入依据当前状态提交一致版本的快照。</summary>
    /// <returns>表示并发状态闭环验证完成的任务。</returns>
    [Test]
    public async Task ConcurrentInputsCommitCurrentStateAndCoherentSnapshot()
    {
        EditorFeature feature = new();
        Task[] writers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (int index = 0; index < 125; index++)
            {
                feature.SetDelta(1);
            }
        })).ToArray();
        bool inconsistent = false;
        while (writers.Any(static task => !task.IsCompleted))
        {
            RuntimeSnapshot<EditorState> snapshot = feature.Snapshot;
            inconsistent |= snapshot.Version != snapshot.State.Total || !snapshot.OperationStates.IsEmpty;
            await Task.Yield();
        }

        await Task.WhenAll(writers);
        await Assert.That(inconsistent).IsFalse();
        await Assert.That(feature.Snapshot.State.Total).IsEqualTo(1000);
        await Assert.That(feature.Snapshot.Version).IsEqualTo(1000L);
    }

    /// <summary>验证同实例规则重入不能提交嵌套输入。</summary>
    /// <returns>表示重入保护验证完成的任务。</returns>
    [Test]
    public async Task SameInstanceRuleReentryCannotCommitNestedInput()
    {
        ReentrantFeature feature = new();
        RuntimeSnapshot<EditorState> initial = feature.Snapshot;
        ReentrantFeature.Reenter = () => feature.SetName("nested");
        Exception? thrown = null;
        try
        {
            feature.SetDelta(1);
        }
        catch (InvalidOperationException exception)
        {
            thrown = exception;
        }
        finally
        {
            ReentrantFeature.Reenter = null;
        }

        await Assert.That(thrown).IsNotNull();
        await Assert.That(ReferenceEquals(feature.Snapshot, initial)).IsTrue();
        feature.SetName("after failure");
        await Assert.That(feature.Snapshot.Version).IsEqualTo(1L);
    }

    /// <summary>验证自定义规则只处理有关输入并使用提交时的当前状态。</summary>
    /// <returns>表示输入规则路由验证完成的任务。</returns>
    [Test]
    public async Task CustomRuleUsesCurrentStateAndOnlyItsInput()
    {
        EditorFeature feature = new();
        feature.SetName("draft");
        feature.SetDelta(2);
        feature.SetDelta(3);
        feature.SetName(string.Empty);

        await Assert.That(feature.Snapshot.State.Total).IsEqualTo(5);
        await Assert.That(feature.Snapshot.State.Name).IsEqualTo(string.Empty);
        await Assert.That(feature.Snapshot.Version).IsEqualTo(4L);
        await Assert.That(typeof(EditorFeature).GetMethod("SetTotal")).IsNull();
    }

    /// <summary>验证普通输入只改变所属实例的业务状态。</summary>
    /// <returns>表示实例隔离验证完成的任务。</returns>
    [Test]
    public async Task OrdinaryInputCommitsOnlyItsInstance()
    {
        EditorFeature first = new();
        EditorFeature second = new();

        first.SetName("editing");

        await Assert.That(first.Snapshot.State.Name).IsEqualTo("editing");
        await Assert.That(first.Snapshot.Version).IsEqualTo(1L);
        await Assert.That(first.Snapshot.OperationStates.IsEmpty).IsTrue();
        await Assert.That(second.Snapshot.State.Name).IsEqualTo(string.Empty);
        await Assert.That(second.Snapshot.Version).IsEqualTo(0L);
    }
}

internal sealed record EditorState
{
    [Input]
    public string Name { get; init; } = string.Empty;

    [Input]
    public int Delta { get; init; }

    public int Total { get; init; }
}

internal sealed partial class EditorFeature() : Feature<EditorState>(new())
{
    [OnInput(nameof(EditorState.Delta))]
    private static EditorState Accumulate(EditorState state, int delta)
        => delta switch
        {
            -1 => throw new InvalidOperationException("规则失败"),
            -2 => null!,
            _ => state with { Delta = delta, Total = state.Total + delta },
        };
}

internal sealed partial class ReentrantFeature() : Feature<EditorState>(new())
{
    internal static Action? Reenter { get; set; }

    [OnInput(nameof(EditorState.Delta))]
    private static EditorState Change(EditorState state, int value)
    {
        Reenter?.Invoke();
        return state with { Delta = value };
    }
}
