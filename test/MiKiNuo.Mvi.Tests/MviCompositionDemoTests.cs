using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;
using TUnit.Assertions;
using TUnit.Core;
using R3;

namespace MiKiNuo.Mvi.Tests;

/// <summary>从生成组合句柄验证完整组合的业务行为及隔离。</summary>
public sealed class MviCompositionDemoTests
{
    /// <summary>同一检索业务独立运行并能嵌入两个互不影响的工作区。</summary>
    [Test]
    public async Task ReusableSearchAndWorkspacesStayIndependentAsync()
    {
        GeneratedMviContainer container = new();
        await using MedicineSearchComposition standalone = await container.CreateMedicineSearchCompositionAsync();
        await using PrescriptionComposition first = await container.CreatePrescriptionCompositionAsync();
        await using PrescriptionComposition second = await container.CreatePrescriptionCompositionAsync();

        await standalone.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Search("甲"));
        await standalone.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Choose("演示药品甲"));
        await first.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Search("乙"));
        await first.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Choose("演示药品乙"));

        await Assert.That(standalone.MedicineSearch.ViewModel.State.CompletedSelections).IsEqualTo(1);
        await Assert.That(first.MedicationDetails.ViewModel.State.Names.Length).IsEqualTo(1);
        await Assert.That(second.MedicationDetails.ViewModel.State.Names.Length).IsEqualTo(0);
        await Assert.That(second.MedicineSearch.ViewModel.State.CompletedSelections).IsEqualTo(0);
    }

    /// <summary>父 MVI 通过范围通知取得摘要，不直接订阅子状态。</summary>
    [Test]
    public async Task ParentReceivesLocalSummaryNotificationAsync()
    {
        GeneratedMviContainer container = new();
        await using PrescriptionComposition composition = await container.CreatePrescriptionCompositionAsync();
        TaskCompletionSource received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = composition.PrescriptionWorkspace.ViewModel.States.Subscribe(state =>
        {
            if (state.LastMedicine == "演示药品甲") received.TrySetResult();
        });

        await composition.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Search("甲"));
        await composition.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Choose("演示药品甲"));

        await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>检索选择经中介者完成明细更新并明确回流。</summary>
    [Test]
    public async Task SelectionUpdatesDetailsAsync()
    {
        GeneratedMviContainer container = new();
        await using PrescriptionComposition composition = await container.CreatePrescriptionCompositionAsync();

        await composition.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Search("甲"));
        await Assert.That(composition.MedicineSearch.ViewModel.State.Results.Length).IsEqualTo(1);
        await composition.MedicineSearch.ViewModel.DispatchAsync(new MedicineSearchIntent.Choose("演示药品甲"));

        await Assert.That(composition.MedicationDetails.ViewModel.State.Names[0]).IsEqualTo("演示药品甲");
        await Assert.That(composition.MedicineSearch.ViewModel.State.CompletedSelections).IsEqualTo(1);
        await Assert.That(composition.MedicineSearch.ViewModel.State.IsBusy).IsFalse();
    }
}
