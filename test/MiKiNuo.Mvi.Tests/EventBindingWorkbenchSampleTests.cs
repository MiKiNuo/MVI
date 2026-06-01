using AvaloniaWorkbench = MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Presentation.Events;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示事件绑定复杂组合示例验证测试。
/// </summary>
public sealed class EventBindingWorkbenchSampleTests
{
    /// <summary>
    /// 验证 Avalonia 示例主窗口暴露事件绑定复杂组合示例入口。
    /// </summary>
    [Test]
    public async Task AvaloniaSampleWindow_ShouldExposeEventBindingWorkbenchEntryPointAsync()
    {
        string repositoryRoot = GetRepositoryRoot();
        string avaloniaSampleRoot = Path.Combine(repositoryRoot, "sample", "MiKiNuo.Mvi.Samples.Avalonia");
        string mainWindowXaml = await File.ReadAllTextAsync(Path.Combine(avaloniaSampleRoot, "MainWindow.axaml"));
        string mainWindowCode = await File.ReadAllTextAsync(Path.Combine(avaloniaSampleRoot, "MainWindow.axaml.cs"));
        string compositionRoot = await File.ReadAllTextAsync(Path.Combine(avaloniaSampleRoot, "Composition", "SampleCompositionRoot.cs"));
        SampleGeneratedViewRegistry generatedViewRegistry = new();

        await Assert.That(mainWindowXaml).Contains("EventBindingWorkbenchButton");
        await Assert.That(mainWindowCode).Contains("ShowEventBindingWorkbenchAsync");
        await Assert.That(compositionRoot).Contains("EventBindingWorkbenchComposition.Create()");
        await Assert.That(generatedViewRegistry).IsNotNull();
    }

    /// <summary>
    /// 验证 Avalonia 复杂组合示例中每个子 ViewModel 独立接收 ViewEvent，并通过 Mediator 通知父组合。
    /// </summary>
    [Test]
    public async Task AvaloniaWorkbench_Should_RouteChildViewEventsThroughIndependentStoresAndMediatorAsync()
    {
        await using AvaloniaWorkbench.EventBindingWorkbenchComposition composition = AvaloniaWorkbench.EventBindingWorkbenchComposition.Create();

        composition.SearchViewModel.QueryTextChangedCommand.Execute(new MviTextChangedEventPayload("张三", string.Empty, true, null));
        composition.SelectionViewModel.SelectionChangedCommand.Execute(new MviSelectionChangedEventPayload("P10001", 0, null, null));
        composition.DetailViewModel.DetailPressedCommand.Execute(new MviPointerEventPayload(
            24,
            48,
            MviPointerButton.Left,
            1,
            true,
            MviInputModifiers.Control,
            null));
        composition.DetailViewModel.RefreshCommand.Execute(new MviActionEventPayload("DetailRefreshButton", "Refresh", null));
        await Task.Delay(100);

        await Assert.That(composition.SearchStore.CurrentState.QueryText).IsEqualTo("张三");
        await Assert.That(composition.SelectionStore.CurrentState.SelectedPatientId).IsEqualTo("P10001");
        await Assert.That(composition.DetailStore.CurrentState.LastPointerText).IsEqualTo("Pointer Left @ 24,48");
        await Assert.That(composition.DetailStore.CurrentState.RefreshCount).IsEqualTo(1);
        await Assert.That(composition.WorkbenchStore.CurrentState.InteractionCount).IsEqualTo(4);
        await Assert.That(composition.Mediator.RecordedRequests.Count).IsEqualTo(4);
    }

    /// <summary>
    /// 验证 Godot 复杂组合示例使用 BindEvent 帮助方法绑定子 View 自带事件。
    /// </summary>
    [Test]
    public async Task GodotWorkbench_Should_ProvideIndependentChildViewsUsingBindEventHelpersAsync()
    {
        string sampleRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "sample",
            "MiKiNuo.Mvi.Samples.Godot"));
        string searchViewPath = Path.Combine(sampleRoot, "Views", "EventBindingWorkbench", "SearchPanel", "EventBindingSearchPanelView.cs");
        string selectionViewPath = Path.Combine(sampleRoot, "Views", "EventBindingWorkbench", "SelectionPanel", "EventBindingSelectionPanelView.cs");
        string detailViewPath = Path.Combine(sampleRoot, "Views", "EventBindingWorkbench", "DetailPanel", "EventBindingDetailPanelView.cs");

        string searchView = await File.ReadAllTextAsync(searchViewPath);
        string selectionView = await File.ReadAllTextAsync(selectionViewPath);
        string detailView = await File.ReadAllTextAsync(detailViewPath);

        await Assert.That(searchView).Contains("BindEvent<string, LineEdit.TextChangedEventHandler>");
        await Assert.That(searchView).Contains("QueryTextChangedCommand");
        await Assert.That(selectionView).Contains("BindEvent<long, ItemList.ItemSelectedEventHandler>");
        await Assert.That(selectionView).Contains("SelectionChangedCommand");
        await Assert.That(detailView).Contains("BindEvent(");
        await Assert.That(detailView).Contains("PrepareCommand");
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
    }
}
