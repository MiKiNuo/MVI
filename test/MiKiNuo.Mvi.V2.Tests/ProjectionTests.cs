using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过公开生成投影验证提交、字段通知和调度行为。</summary>
public sealed class ProjectionTests
{
    /// <summary>验证 View 输入规范化为旧显示值时反馈输入字段，后台同值及派生字段仍不重复通知。</summary>
    /// <returns>表示输入反馈验证完成的任务。</returns>
    [Test]
    public async Task ViewInputAcknowledgesUnchangedNormalizedValueWithoutRefreshingUnrelatedFields()
    {
        ProjectionEditor feature = new();
        feature.SetText("Ada");
        Queue<Action> scheduled = new();
        using ProjectionEditor.Projection view = feature.CreateProjection(scheduled.Enqueue);
        List<string> changes = [];
        view.PropertyChanged += (_, args) => changes.Add(args.PropertyName!);
        view.Text = "  Ada  ";
        scheduled.Dequeue()();
        await Assert.That(feature.Snapshot.Version).IsEqualTo(2);
        await Assert.That(view.Text).IsEqualTo("Ada");
        await Assert.That(string.Join(",", changes)).IsEqualTo("Text,Snapshot");
        changes.Clear();
        feature.SetText("  Ada  ");
        scheduled.Dequeue()();
        await Assert.That(string.Join(",", changes)).IsEqualTo("Snapshot");
    }

    /// <summary>验证连接与后台提交同时开始时初始快照与后续逐次展示之间没有缺口。</summary>
    /// <returns>表示并发连接验证完成的任务。</returns>
    [Test]
    public async Task ConnectingDuringCommitsDoesNotMissOrRepeatVersions()
    {
        ProjectionEditor feature = new();
        ConcurrentQueue<Action> scheduled = new();
        using Barrier start = new(2);
        Task inputs = Task.Run(() =>
        {
            start.SignalAndWait(TimeSpan.FromSeconds(10));
            for (int index = 1; index <= 1000; index++)
            {
                feature.SetOther(index);
            }
        });
        start.SignalAndWait(TimeSpan.FromSeconds(10));
        using ProjectionEditor.Projection view = feature.CreateProjection(scheduled.Enqueue, ProjectionMode.EveryCommit);
        long initialVersion = view.Snapshot.Version;
        List<long> versions = [];
        view.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(view.Snapshot))
            {
                versions.Add(view.Snapshot.Version);
            }
        };
        await inputs.WaitAsync(TimeSpan.FromSeconds(10));
        while (scheduled.TryDequeue(out Action? display))
        {
            display();
        }

        await Assert.That(view.Other).IsEqualTo(1000);
        await Assert.That(string.Join(",", versions)).IsEqualTo(string.Join(",", Enumerable.Range((int)initialVersion + 1, 1000 - (int)initialVersion)));
    }

    /// <summary>验证合并回调在一次展示后归还消息泵。</summary>
    /// <returns>表示验证完成的任务。</returns>
    [Test]
    public async Task CoalescingYieldsAfterOneSnapshotWithoutDroppingInputs()
    {
        ProjectionEditor feature = new();
        Queue<Action> scheduled = new();
        using ProjectionEditor.Projection view = feature.CreateProjection(scheduled.Enqueue);
        List<long> versions = [];
        view.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(view.Snapshot))
            {
                versions.Add(view.Snapshot.Version);
                if (view.Snapshot.Version == 1)
                {
                    feature.SetText("second");
                }
            }
        };
        feature.SetText("first");
        scheduled.Dequeue()();
        await Assert.That(feature.Snapshot.Version).IsEqualTo(2);
        await Assert.That(view.Text).IsEqualTo("first");
        await Assert.That(scheduled.Count).IsEqualTo(1);
        scheduled.Dequeue()();
        await Assert.That(string.Join(",", versions)).IsEqualTo("1,2");
    }

    /// <summary>验证普通输入、中间值与按有关字段通知。</summary>
    /// <returns>表示验证完成的任务。</returns>
    [Test]
    public async Task ProjectionShowsCommittedValuesAndOnlyChangedFields()
    {
        ProjectionEditor feature = new();
        Queue<Action> scheduled = new();
        using ProjectionEditor.Projection view = feature.CreateProjection(scheduled.Enqueue);
        List<string> changes = [];
        view.PropertyChanged += (_, args) => changes.Add(args.PropertyName!);
        view.Text = "  -  ";
        await Assert.That(feature.Snapshot.State.Text).IsEqualTo("-");
        await Assert.That(view.Text).IsEqualTo("");
        scheduled.Dequeue()();
        await Assert.That(view.Label).IsEqualTo("Value:-");
        await Assert.That(string.Join(",", changes)).IsEqualTo("Label,Text,Snapshot");
        changes.Clear();
        feature.SetOther(1);
        scheduled.Dequeue()();
        await Assert.That(string.Join(",", changes)).IsEqualTo("Other,Snapshot");
        changes.Clear();
        feature.SetText("-");
        scheduled.Dequeue()();
        await Assert.That(string.Join(",", changes)).IsEqualTo("Snapshot");
    }

    /// <summary>验证提交并发且门外投递先后反转时两种展示方式均保持版本顺序。</summary>
    /// <param name="mode">本地展示方式。</param>
    /// <returns>表示验证完成的任务。</returns>
    [Test]
    [Arguments(ProjectionMode.Coalesce)]
    [Arguments(ProjectionMode.EveryCommit)]
    public async Task ConcurrentCommitsRemainOrderedWithOneScheduledCallback(ProjectionMode mode)
    {
        ProjectionEditor feature = new();
        ConcurrentQueue<Action> scheduled = new();
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        using ProjectionEditor.Projection view = feature.CreateProjection(action =>
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("投影调度屏障超时。");
            }

            scheduled.Enqueue(action);
        }, mode);
        List<long> versions = [];
        view.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(view.Snapshot))
            {
                versions.Add(view.Snapshot.Version);
            }
        };
        Task first = Task.Run(() => feature.SetText("1"));
        await Assert.That(entered.Wait(TimeSpan.FromSeconds(10))).IsTrue();
        await Task.Run(() =>
        {
            for (int index = 2; index <= 100; index++)
            {
                feature.SetText(index.ToString());
            }
        }).WaitAsync(TimeSpan.FromSeconds(10));
        release.Set();
        await first.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(scheduled.Count).IsEqualTo(1);
        await Assert.That(feature.Snapshot.Version).IsEqualTo(100);
        scheduled.TryDequeue(out Action? display);
        display!();
        await Assert.That(view.Snapshot.Version).IsEqualTo(100);
        await Assert.That(string.Join(",", versions)).IsEqualTo(mode == ProjectionMode.Coalesce
            ? "100" : string.Join(",", Enumerable.Range(1, 100)));
    }

    /// <summary>验证平台通知在提交门外且连接、释放及重连保持一致快照。</summary>
    /// <returns>表示验证完成的任务。</returns>
    [Test]
    public async Task ConnectionStartsAtCommittedSnapshotAndDisposalInvalidatesOldView()
    {
        ProjectionEditor feature = new();
        feature.SetText("initial");
        Queue<Action> scheduled = new();
        ProjectionEditor.Projection view = feature.CreateProjection(scheduled.Enqueue);
        await Assert.That(view.Text).IsEqualTo("initial");
        await Assert.That(view.Snapshot.Version).IsEqualTo(1);
        await Assert.That(() => feature.CreateProjection(scheduled.Enqueue)).Throws<InvalidOperationException>();
        view.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(view.Text))
            {
                Task.Run(() => feature.SetOther(1)).WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
        };
        feature.SetText("next");
        scheduled.Dequeue()();
        await Assert.That(feature.Snapshot.Version).IsEqualTo(3);
        view.Dispose();
        scheduled.Dequeue()();
        await Assert.That(() => view.Text = "old").Throws<ObjectDisposedException>();
        using ProjectionEditor.Projection replacement = feature.CreateProjection(scheduled.Enqueue);
        await Assert.That(replacement.Text).IsEqualTo("next");
        await Assert.That(replacement.Other).IsEqualTo(1);
    }

    /// <summary>验证调度失败和平台通知异常后下一次输入仍可展示。</summary>
    /// <returns>表示验证完成的任务。</returns>
    [Test]
    public async Task SchedulingAndNotificationFailuresDoNotPermanentlyStallProjection()
    {
        ProjectionEditor feature = new();
        Queue<Action> scheduled = new();
        bool failSchedule = true;
        bool failNotification = true;
        using ProjectionEditor.Projection view = feature.CreateProjection(action =>
        {
            if (failSchedule)
            {
                failSchedule = false;
                throw new InvalidOperationException("平台调度暂时失败。");
            }

            scheduled.Enqueue(action);
        });
        view.PropertyChanged += (_, _) =>
        {
            if (failNotification)
            {
                failNotification = false;
                throw new InvalidOperationException("绑定处理故障。");
            }
        };
        await Assert.That(() => feature.SetText("1")).Throws<InvalidOperationException>();
        await Assert.That(feature.Snapshot.Version).IsEqualTo(1);
        feature.SetText("2");
        await Assert.That(() => scheduled.Dequeue()()).Throws<InvalidOperationException>();
        feature.SetText("3");
        scheduled.Dequeue()();
        await Assert.That(view.Text).IsEqualTo("3");
        await Assert.That(view.Snapshot.Version).IsEqualTo(3);
    }
}

internal sealed record ProjectionState
{
    /// <summary>获取接受纯规则转换的编辑文本。</summary>
    [Input]
    public string Text { get; init; } = "";
    /// <summary>获取与文本无关的编辑字段。</summary>
    [Input]
    public int Other { get; init; }
    /// <summary>获取从提交文本派生的只读标签。</summary>
    public string Label => "Value:" + Text;
}

internal sealed partial class ProjectionEditor() : Feature<ProjectionState>(new())
{
    [OnInput(nameof(ProjectionState.Text))]
    private static ProjectionState Normalize(ProjectionState state, string value) => state with { Text = value.Trim() };
}
