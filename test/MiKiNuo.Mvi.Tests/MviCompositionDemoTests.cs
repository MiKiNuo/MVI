using MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;
using TUnit.Assertions;
using TUnit.Core;
using R3;

namespace MiKiNuo.Mvi.Tests;

/// <summary>从公共宿主验证完整组合的业务行为及隔离。</summary>
public sealed class MviCompositionDemoTests
{
    /// <summary>同一检索业务独立运行并能嵌入两个互不影响的工作区。</summary>
    [Test]
    public async Task ReusableSearchAndWorkspacesStayIndependentAsync()
    {
        await using MedicineSearchDemoHost standalone = new();
        await using PrescriptionDemoHost first = new();
        await using PrescriptionDemoHost second = new();
        await standalone.Search.DispatchAsync(new MedicineSearchIntent.Search("甲"));
        await standalone.Search.DispatchAsync(new MedicineSearchIntent.Choose("演示药品甲"));
        await first.Search.DispatchAsync(new MedicineSearchIntent.Search("乙"));
        await first.Search.DispatchAsync(new MedicineSearchIntent.Choose("演示药品乙"));
        await Assert.That(standalone.Search.State.CompletedSelections).IsEqualTo(1);
        await Assert.That(first.Details.State.Names.Length).IsEqualTo(1);
        await Assert.That(second.Details.State.Names.Length).IsEqualTo(0);
        await Assert.That(second.Search.State.CompletedSelections).IsEqualTo(0);
    }

    /// <summary>父 MVI 通过范围通知取得摘要，不直接订阅子状态。</summary>
    [Test]
    public async Task ParentReceivesLocalSummaryNotificationAsync()
    {
        await using PrescriptionDemoHost host = new();
        TaskCompletionSource received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = host.Workspace.States.Subscribe(state =>
        {
            if (state.LastMedicine == "演示药品甲") received.TrySetResult();
        });
        await host.Search.DispatchAsync(new MedicineSearchIntent.Search("甲"));
        await host.Search.DispatchAsync(new MedicineSearchIntent.Choose("演示药品甲"));
        await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
    /// <summary>检索选择经中介者完成明细更新并明确回流。</summary>
    [Test]
    public async Task SelectionUpdatesDetailsAsync()
    {
        await using PrescriptionDemoHost host = new();
        await host.Search.DispatchAsync(new MedicineSearchIntent.Search("甲"));
        await Assert.That(host.Search.State.Results.Length).IsEqualTo(1);
        await host.Search.DispatchAsync(new MedicineSearchIntent.Choose("演示药品甲"));
        await Assert.That(host.Details.State.Names[0]).IsEqualTo("演示药品甲");
        await Assert.That(host.Search.State.CompletedSelections).IsEqualTo(1);
        await Assert.That(host.Search.State.IsBusy).IsFalse();
    }
}
