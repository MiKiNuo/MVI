using AvaloniaWorkbench = MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;
using SharedWorkbench = MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
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
        SampleGeneratedContainer container = new();
        SampleGeneratedViewRegistry generatedViewRegistry = new(container);

        await Assert.That(mainWindowXaml).Contains("EventBindingWorkbenchButton");
        await Assert.That(mainWindowCode).Contains("ShowEventBindingWorkbenchAsync");
        await Assert.That(compositionRoot).Contains("EventBindingWorkbenchComposition.Create()");
        await Assert.That(generatedViewRegistry).IsNotNull();
    }

    /// <summary>
    /// 验证 Avalonia 复杂组合示例中每个子 Store 独立接收 Intent，并通过 Mediator 通知父组合。
    /// <para>
    /// 新事件绑定模式下事件直接映射为 Intent 派发到 Store（不经过命令层），
    /// 本测试直接向各子 Store 派发 Intent 验证状态流转与中介者通知。
    /// </para>
    /// </summary>
    [Test]
    public async Task AvaloniaWorkbench_Should_RouteChildIntentsThroughIndependentStoresAndMediatorAsync()
    {
        await using AvaloniaWorkbench.EventBindingWorkbenchComposition composition = AvaloniaWorkbench.EventBindingWorkbenchComposition.Create();

        await composition.SearchStore.DispatchAsync(new SharedWorkbench.EventBindingSearchIntent.ChangeQuery(
            new MviTextChangedEventPayload("张三", string.Empty, true, null)));
        await composition.SelectionStore.DispatchAsync(new AvaloniaWorkbench.EventBindingSelectionIntent.ChangeSelection(
            new MviSelectionChangedEventPayload("P10001", 0, null, null)));
        await composition.DetailStore.DispatchAsync(new AvaloniaWorkbench.EventBindingDetailIntent.PressDetail(
            new MviPointerEventPayload(
                24,
                48,
                MviPointerButton.Left,
                1,
                true,
                MviInputModifiers.Control,
                null)));
        await composition.DetailStore.DispatchAsync(new AvaloniaWorkbench.EventBindingDetailIntent.Refresh(
            new MviActionEventPayload("DetailRefreshButton", "Refresh", null)));
        await Task.Delay(100);

        await Assert.That(composition.SearchStore.CurrentState.QueryText).IsEqualTo("张三");
        await Assert.That(composition.SelectionStore.CurrentState.SelectedPatientId).IsEqualTo("P10001");
        await Assert.That(composition.DetailStore.CurrentState.LastPointerText).IsEqualTo("Pointer Left @ 24,48");
        await Assert.That(composition.DetailStore.CurrentState.RefreshCount).IsEqualTo(1);
        await Assert.That(composition.WorkbenchStore.CurrentState.InteractionCount).IsEqualTo(4);
        await Assert.That(composition.Mediator.RecordedRequests.Count).IsEqualTo(4);
    }

    /// <summary>
    /// 验证 Avalonia 复杂组合示例的子 ViewModel 不再暴露命令属性（事件绑定改为 IEventSource + EventBinding 模式）。
    /// </summary>
    [Test]
    public async Task AvaloniaWorkbench_ChildViewModels_Should_NotExposeCommandPropertiesAsync()
    {
        string repositoryRoot = GetRepositoryRoot();
        string modelsPath = Path.Combine(repositoryRoot, "sample", "MiKiNuo.Mvi.Samples.Avalonia", "Features", "EventBindingWorkbench", "EventBindingWorkbenchModels.cs");
        string models = await File.ReadAllTextAsync(modelsPath);

        await Assert.That(models).DoesNotContain("QueryTextChangedCommand");
        await Assert.That(models).DoesNotContain("SelectionChangedCommand");
        await Assert.That(models).DoesNotContain("DetailPressedCommand");
        await Assert.That(models).DoesNotContain("RefreshCommand");
        await Assert.That(models).DoesNotContain("InitializeGeneratedCommands");
    }

    /// <summary>
    /// 验证 Avalonia 复杂组合示例的子 View 使用源生成器扩展方法 + EventBinding 模式绑定事件。
    /// </summary>
    [Test]
    public async Task AvaloniaWorkbench_ChildViews_Should_UseEventSourceAdapterPatternAsync()
    {
        string repositoryRoot = GetRepositoryRoot();
        string workbenchDir = Path.Combine(repositoryRoot, "sample", "MiKiNuo.Mvi.Samples.Avalonia", "Features", "EventBindingWorkbench");

        string searchViewCode = await File.ReadAllTextAsync(Path.Combine(workbenchDir, "EventBindingSearchPanelView.axaml.cs"));
        string selectionViewCode = await File.ReadAllTextAsync(Path.Combine(workbenchDir, "EventBindingSelectionPanelView.axaml.cs"));
        string detailViewCode = await File.ReadAllTextAsync(Path.Combine(workbenchDir, "EventBindingDetailPanelView.axaml.cs"));

        await Assert.That(searchViewCode).Contains("ToEventSource().TextChanged");
        await Assert.That(searchViewCode).Contains("BindTo");
        await Assert.That(searchViewCode).Contains("GetIntentDispatcher");

        await Assert.That(selectionViewCode).Contains("ToEventSource().SelectionChanged");
        await Assert.That(selectionViewCode).Contains("BindTo");
        await Assert.That(selectionViewCode).Contains("GetIntentDispatcher");

        await Assert.That(detailViewCode).Contains("ToEventSource().PointerPressed");
        await Assert.That(detailViewCode).Contains("ToEventSource().Click");
        await Assert.That(detailViewCode).Contains("BindTo");
    }

    /// <summary>
    /// 验证 Godot 复杂组合示例使用源生成器扩展方法 + EventBinding 模式绑定子 View 事件。
    /// </summary>
    [Test]
    public async Task GodotWorkbench_Should_ProvideIndependentChildViewsUsingEventSourceAdaptersAsync()
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

        await Assert.That(searchView).Contains("ToEventSource().TextChanged");
        await Assert.That(searchView).Contains("BindTo");
        await Assert.That(searchView).Contains("GetIntentDispatcher");

        await Assert.That(selectionView).Contains("ToEventSource().ItemSelected");
        await Assert.That(selectionView).Contains("BindTo");
        await Assert.That(selectionView).Contains("GetIntentDispatcher");

        await Assert.That(detailView).Contains("ToEventSource().Pressed");
        await Assert.That(detailView).Contains("BindTo");
        await Assert.That(detailView).Contains("GetIntentDispatcher");
    }

    /// <summary>
    /// 验证记录型中介者对不支持的请求类型抛出明确异常。
    /// </summary>
    [Test]
    public async Task AvaloniaWorkbench_Mediator_Should_ThrowForUnsupportedRequestTypeAsync()
    {
        await using AvaloniaWorkbench.EventBindingWorkbenchComposition composition = AvaloniaWorkbench.EventBindingWorkbenchComposition.Create();

        InvalidOperationException? ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await composition.Mediator.SendAsync<string, SharedWorkbench.EventBindingWorkbenchInteractionResponse>("bad-request");
        });

        await Assert.That(ex!.Message).Contains("不支持请求类型");
    }

    /// <summary>
    /// 验证记录型中介者对不兼容的响应类型抛出明确异常。
    /// </summary>
    [Test]
    public async Task AvaloniaWorkbench_Mediator_Should_ThrowForIncompatibleResponseTypeAsync()
    {
        await using AvaloniaWorkbench.EventBindingWorkbenchComposition composition = AvaloniaWorkbench.EventBindingWorkbenchComposition.Create();

        InvalidOperationException? ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await composition.Mediator.SendAsync<SharedWorkbench.EventBindingWorkbenchInteractionRequest, string>(
                new SharedWorkbench.EventBindingWorkbenchInteractionRequest("Test", "Probe", "ctx"));
        });

        await Assert.That(ex!.Message).Contains("无法将响应转换为请求类型");
    }

    /// <summary>
    /// 验证记录型中介者构造时注入 Store，消除两阶段初始化。
    /// </summary>
    [Test]
    public async Task AvaloniaWorkbench_Mediator_Should_DispatchToWorkbenchStoreImmediatelyAsync()
    {
        await using AvaloniaWorkbench.EventBindingWorkbenchComposition composition = AvaloniaWorkbench.EventBindingWorkbenchComposition.Create();

        await composition.Mediator.SendAsync<SharedWorkbench.EventBindingWorkbenchInteractionRequest, SharedWorkbench.EventBindingWorkbenchInteractionResponse>(
            new SharedWorkbench.EventBindingWorkbenchInteractionRequest("Test", "Probe", "ctx"));
        await Task.Delay(50);

        await Assert.That(composition.WorkbenchStore.CurrentState.InteractionCount).IsEqualTo(1);
        await Assert.That(composition.Mediator.RecordedRequests.Count).IsEqualTo(1);
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
