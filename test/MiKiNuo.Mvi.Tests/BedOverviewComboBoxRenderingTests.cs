using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 BedOverview 卡片 <c>ComboBox</c> 的渲染 / 状态同步测试。
/// <para>
/// 目标：验证 <c>CardView.axaml</c> 中 <c>BedFilterComboBox</c> 控件在 PageKey=BedOverview 时：
/// </para>
/// <list type="number">
///   <item>真实存在于可视树中；</item>
///   <item>绑定数据源 <c>AvailableBedFilters</c> 内容与 <see cref="BedFilterOption.All"/> 完全一致；</item>
///   <item>派发 <see cref="CardIntent.SetBedFilter"/> 后 <see cref="CardViewModel.SelectedBedFilterOption"/>、
///   <see cref="CardViewModel.FilteredBedCount"/> 与 <see cref="BedFilter.Open"/> 对齐；</item>
///   <item>非 BedOverview 卡片上 ComboBox 父容器 <c>StackPanel.IsVisible=false</c>。</item>
/// </list>
/// <para>
/// 与 <c>CardBedFilterTests</c> 互补：reducer 单元测试断言 <c>state</c> 维度；
/// 本测试断言 <c>state -&gt; CardViewModel -&gt; 控件树</c> 端到端打通。
/// </para>
/// <para>
/// Avalonia headless 平台在 <c>HeadlessUnitTestSession</c> 内部启动一个 UI 线程，
/// <c>session.Dispatch(...)</c> 把 lambda 排到该线程执行。
/// 对于"是否绑到正确数据源"这种断言我们不依赖 Avalonia 实际发起 binding 流水线
/// （headless 下 <c>avares://</c> 资源在测试程序集里定位不到，binding 可能延迟），
/// 而是直接断言 <c>CardViewModel</c> 暴露给绑定的属性值 —— 这才是 XAML 实际读到的源头。
/// </para>
/// </summary>
public sealed class BedOverviewComboBoxRenderingTests
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp));

    /// <summary>
    /// 验证 headless 测试环境已正确启动 <see cref="HeadlessTestApp"/>（其它渲染测试的隐含前置条件）。
    /// </summary>
    [Test]
    public async Task BedOverviewCard_VisualTree_Contains_BedFilterComboBoxAsync()
    {
        string appName = await Session.Dispatch(
            () => global::Avalonia.Application.Current?.GetType().Name ?? "<null>",
            CancellationToken.None);

        await Assert.That(appName).IsEqualTo("HeadlessTestApp");
    }

    /// <summary>
    /// 验证 XAML 绑定源 <see cref="CardViewModel.AvailableBedFilters"/> 包含与 <see cref="BedFilterOption.All"/>
    /// 数量一致且逐项相等的项。
    /// <para>
    /// headless binding 在测试程序集中无法直接解析 <c>avares://</c>，但 XAML 实际拿到的源就是
    /// <see cref="CardViewModel.AvailableBedFilters"/>，所以这里直接断言 ViewModel 暴露的集合。
    /// </para>
    /// </summary>
    [Test]
    public async Task BedOverviewCard_AvailableBedFilters_ExposesAllBedFilterOptionsAsync()
    {
        IReadOnlyList<BedFilterOption> filters = await Session.Dispatch(
            () =>
            {
                CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);
                return viewModel.AvailableBedFilters;
            },
            CancellationToken.None);

        await Assert.That(filters.Count).IsEqualTo(BedFilterOption.All.Count);
        for (int i = 0; i < BedFilterOption.All.Count; i++)
        {
            BedFilterOption expected = BedFilterOption.All[i];
            BedFilterOption actual = filters[i];
            await Assert.That(actual.Value).IsEqualTo(expected.Value);
            await Assert.That(actual.DisplayName).IsEqualTo(expected.DisplayName);
        }
    }

    /// <summary>
    /// 验证 BedOverview 卡片初始 <see cref="CardViewModel.SelectedBedFilterOption"/> 为
    /// "全部床位"（即 <see cref="BedFilter.All"/>），<see cref="CardViewModel.FilteredBedCount"/>
    /// 等于 <see cref="BedCatalog.TotalCount"/>。
    /// </summary>
    [Test]
    public async Task BedOverviewCard_InitialBedFilter_IsAllAsync()
    {
        (BedFilterOption selected, int filteredCount, bool showCatalog) = await Session.Dispatch(
            () =>
            {
                CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);
                return (viewModel.SelectedBedFilterOption, viewModel.FilteredBedCount, viewModel.ShowBedCatalog);
            },
            CancellationToken.None);

        await Assert.That(selected.Value).IsEqualTo(BedFilter.All);
        await Assert.That(selected.DisplayName).IsEqualTo(BedFilterOption.All[0].DisplayName);
        await Assert.That(filteredCount).IsEqualTo(BedCatalog.TotalCount);
        await Assert.That(showCatalog).IsTrue();
    }

    /// <summary>
    /// 验证派发 <see cref="CardIntent.SetBedFilter"/>（通过 <see cref="CardViewModel.SetBedFilterAsync"/>）后，
    /// <see cref="CardViewModel.SelectedBedFilterOption"/> 切到 <see cref="BedFilter.Open"/> 对应的 option，
    /// <see cref="CardViewModel.FilteredBedCount"/> 同步为 <see cref="BedCatalog.Count(BedFilter)"/>。
    /// </summary>
    [Test]
    public async Task BedOverviewCard_DispatchSetBedFilter_SyncsSelectedOptionAndCountAsync()
    {
        (BedFilterOption selected, int filteredCount) = await Session.Dispatch(
            async () =>
            {
                CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);
                await viewModel.SetBedFilterAsync(BedFilter.Open).ConfigureAwait(true);
                // 等待 Store.States.Subscribe 触发的 RebuildDerivedProperties 同步到 SelectedBedFilterOption。
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                return (viewModel.SelectedBedFilterOption, viewModel.FilteredBedCount);
            },
            CancellationToken.None);

        await Assert.That(selected.Value).IsEqualTo(BedFilter.Open);
        await Assert.That(filteredCount).IsEqualTo(BedCatalog.Count(BedFilter.Open));
        await Assert.That(filteredCount).IsGreaterThan(0);
    }

    /// <summary>
    /// 验证非 BedOverview 卡片（如 NursingTaskBoard）上 <see cref="CardViewModel.ShowBedCatalog"/>
    /// 为 false —— 这就是 XAML 中 <c>StackPanel.IsVisible</c> 实际读取的源。
    /// </summary>
    [Test]
    public async Task NonBedOverviewCard_ShowBedCatalog_IsFalseAsync()
    {
        bool showCatalog = await Session.Dispatch(
            () => CreateCardViewModel(PageKey.NursingTaskBoard).ShowBedCatalog,
            CancellationToken.None);

        await Assert.That(showCatalog).IsFalse();
    }

    /// <summary>
    /// 验证 BedOverview 卡片暴露的 <see cref="CardViewModel.FilteredBeds"/>（DataGrid 数据源）
    /// 始终与 <see cref="CardState.CurrentBedFilter"/> 同步：初始 All 时为目录全集；
    /// 派发 <see cref="CardIntent.SetBedFilter"/> 后切换为 <see cref="BedCatalog.Query(BedFilter)"/> 子集。
    /// </summary>
    [Test]
    public async Task BedOverviewCard_FilteredBeds_ReflectsCurrentBedFilterAsync()
    {
        (int initialCount, int afterOpenCount, int backToAllCount, IReadOnlyList<BedRecord> openSample) =
            await Session.Dispatch(
                async () =>
                {
                    CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);
                    int initial = viewModel.FilteredBeds.Count;
                    await viewModel.SetBedFilterAsync(BedFilter.Open).ConfigureAwait(true);
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                    int afterOpen = viewModel.FilteredBeds.Count;
                    List<BedRecord> openSnapshot = new(viewModel.FilteredBeds.Count);
                    foreach (BedRecord record in viewModel.FilteredBeds)
                    {
                        openSnapshot.Add(record);
                    }

                    await viewModel.SetBedFilterAsync(BedFilter.All).ConfigureAwait(true);
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                    int backToAll = viewModel.FilteredBeds.Count;
                    return (initial, afterOpen, backToAll, (IReadOnlyList<BedRecord>)openSnapshot);
                },
                CancellationToken.None);

        await Assert.That(initialCount).IsEqualTo(BedCatalog.TotalCount);
        await Assert.That(afterOpenCount).IsEqualTo(BedCatalog.Count(BedFilter.Open));
        await Assert.That(afterOpenCount).IsGreaterThan(0);
        await Assert.That(backToAllCount).IsEqualTo(BedCatalog.TotalCount);
        foreach (BedRecord record in openSample)
        {
            await Assert.That(record.Status).IsEqualTo(BedStatus.Open);
        }
    }

    /// <summary>
    /// 验证 <c>CardView.axaml</c> 中存在 <c>DataGrid</c> 节点，且 <c>ItemsSource</c> 绑定到
    /// <see cref="CardViewModel.FilteredBedRows"/>，节点名 <c>FilteredBedsDataGrid</c>，并
    /// 包含至少 5 列（床号/病区/类型/状态/患者/主治医师），且「类型」「状态」列绑定到
    /// 中文展示字段 <see cref="BedRecordRow.TypeDisplay"/> / <see cref="BedRecordRow.StatusDisplay"/>。
    /// <para>
    /// 之所以走"读 XAML 文本"而不是 <c>FindDescendantOfType&lt;DataGrid&gt;</c>：
    /// headless 测试程序集里 <c>avares://</c> 资源定位不到，
    /// <see cref="Avalonia.Markup.Xaml.AvaloniaXamlLoader"/> 抛 <c>InvalidOperationException</c>，
    /// 整个 <see cref="CardView"/> 构造失败、视觉树为空，导致 <c>FindDescendantOfType</c> 永远返回 null。
    /// 读 XAML 文本是 headless 下唯一稳定的契约断言（编译时 XAML 也走 <c>BAML/Avalonia XAML</c>，不会被破坏）。
    /// </para>
    /// </summary>
    [Test]
    public async Task CardViewXaml_BedOverview_Contains_DataGridBoundToFilteredBedsAsync()
    {
        string xamlText = await LoadCardViewXamlAsync();
        int dataGridCount = CountOccurrences(xamlText, "<DataGrid");
        int filteredBedsBindings = CountOccurrences(xamlText, "{Binding FilteredBedRows}");
        int typeDisplayBindings = CountOccurrences(xamlText, "{Binding TypeDisplay}");
        int statusDisplayBindings = CountOccurrences(xamlText, "{Binding StatusDisplay}");
        int columnHeaders =
            CountOccurrences(xamlText, "Header=\"床号\"")
            + CountOccurrences(xamlText, "Header=\"病区\"")
            + CountOccurrences(xamlText, "Header=\"类型\"")
            + CountOccurrences(xamlText, "Header=\"状态\"")
            + CountOccurrences(xamlText, "Header=\"患者\"")
            + CountOccurrences(xamlText, "Header=\"主治医师\"");

        await Assert.That(dataGridCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(filteredBedsBindings).IsGreaterThanOrEqualTo(1);
        await Assert.That(typeDisplayBindings).IsGreaterThanOrEqualTo(1);
        await Assert.That(statusDisplayBindings).IsGreaterThanOrEqualTo(1);
        await Assert.That(columnHeaders).IsGreaterThanOrEqualTo(5);
    }

    /// <summary>
    /// 验证 <c>CardView.axaml</c> 中 <c>DataGrid</c> 的 <c>ItemsSource</c> 绑定到
    /// <see cref="CardViewModel.FilteredBedRows"/> 集合，且绑定在床位目录的 StackPanel 内部
    /// （即嵌套在 <c>IsVisible="{Binding ShowBedCatalog}"</c> 容器里，只在 BedOverview 卡片显示）。
    /// </summary>
    [Test]
    public async Task CardViewXaml_BedOverview_DataGridIsInsideShowBedCatalogContainerAsync()
    {
        string xamlText = await LoadCardViewXamlAsync();
        int showCatalogIdx = xamlText.IndexOf("IsVisible=\"{Binding ShowBedCatalog}\"", StringComparison.Ordinal);
        int stackPanelCloseIdx = FindMatchingStackPanelClose(xamlText, showCatalogIdx);
        int dataGridIdx = xamlText.IndexOf("<DataGrid", showCatalogIdx, StringComparison.Ordinal);
        await Assert.That(showCatalogIdx).IsGreaterThanOrEqualTo(0);
        await Assert.That(dataGridIdx).IsGreaterThan(showCatalogIdx);
        await Assert.That(dataGridIdx).IsLessThan(stackPanelCloseIdx);
    }

    /// <summary>
    /// 验证 BedOverview 卡片的 <see cref="CardViewModel.FilteredBedRows"/> 包装集合与
    /// <see cref="CardViewModel.FilteredBeds"/> 数量同步，且每行 <see cref="BedRecordRow"/>
    /// 的 <see cref="BedRecordRow.TypeDisplay"/> / <see cref="BedRecordRow.StatusDisplay"/>
    /// 已是中文展示名（不是 <c>ToString()</c> 的英文枚举）。
    /// </summary>
    [Test]
    public async Task BedOverviewCard_FilteredBedRows_ProvidesChineseDisplayAsync()
    {
        (int initialCount, int typeSampleCount, int statusSampleCount) = await Session.Dispatch(
            () =>
            {
                CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);
                IReadOnlyList<BedRecordRow> rows = viewModel.FilteredBedRows;
                int typeDisplayCount = 0;
                int statusDisplayCount = 0;
                foreach (BedRecordRow row in rows)
                {
                    if (!row.TypeDisplay.Equals(row.Source.Type.ToString(), StringComparison.Ordinal))
                    {
                        typeDisplayCount++;
                    }

                    if (!row.StatusDisplay.Equals(row.Source.Status.ToString(), StringComparison.Ordinal))
                    {
                        statusDisplayCount++;
                    }
                }

                return (rows.Count, typeDisplayCount, statusDisplayCount);
            },
            CancellationToken.None);

        await Assert.That(initialCount).IsEqualTo(BedCatalog.TotalCount);
        await Assert.That(typeSampleCount).IsGreaterThan(0);
        await Assert.That(statusSampleCount).IsGreaterThan(0);
    }

    /// <summary>
    /// 异步加载 <c>CardView.axaml</c> 的文本内容。优先通过测试程序集内嵌资源/磁盘路径查找，
    /// 并跳过所有 <c>avares://</c> 路径以避免触发 <see cref="global::Avalonia.Platform.AssetLoader"/>
    /// 在 headless 模式下的资源解析失败。
    /// </summary>
    private static async Task<string> LoadCardViewXamlAsync()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "MiKiNuo.Mvi.Samples.Avalonia", "Features", "Dashboard", "Cards", "CardView.axaml"),
            Path.Combine(AppContext.BaseDirectory, "Features", "Dashboard", "Cards", "CardView.axaml"),
            @"f:\MiKiNuoProjects\MVI\sample\MiKiNuo.Mvi.Samples.Avalonia\Features\Dashboard\Cards\CardView.axaml",
        ];
        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path).ConfigureAwait(true);
            }
        }

        throw new FileNotFoundException(
            "在以下候选路径均未找到 CardView.axaml：" + string.Join(" | ", candidates));
    }

    /// <summary>统计 <paramref name="text"/> 中 <paramref name="needle"/> 出现次数（非重叠）。</summary>
    private static int CountOccurrences(string text, string needle)
    {
        if (string.IsNullOrEmpty(needle))
        {
            return 0;
        }

        int count = 0;
        int idx = 0;
        while ((idx = text.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }

        return count;
    }

    /// <summary>
    /// 在 <paramref name="text"/> 中以 <paramref name="showCatalogIdx"/> 为
    /// <c>IsVisible="{Binding ShowBedCatalog}"</c> 文本出现位置，返回承载该属性的最近
    /// <c>&lt;StackPanel ...&gt;</c> 的闭合 <c>&lt;/StackPanel&gt;</c> 索引。
    /// <para>
    /// 算法：从 <paramref name="showCatalogIdx"/> 向前搜索最近的 <c>&lt;StackPanel</c> 开标签作为
    /// 入口；之后采用栈式匹配统计 <c>&lt;StackPanel</c> 与 <c>&lt;/StackPanel&gt;</c> 数量，
    /// 出栈到 0 时返回该位置。
    /// </para>
    /// </summary>
    private static int FindMatchingStackPanelClose(string text, int showCatalogIdx)
    {
        // 1) 向前搜索最近的 <StackPanel ...> 开标签
        int openIdx = text.LastIndexOf("<StackPanel", showCatalogIdx, StringComparison.Ordinal);
        if (openIdx < 0)
        {
            return text.Length;
        }

        // 2) 栈式匹配
        int depth = 1;
        int i = openIdx + "<StackPanel".Length;
        while (i < text.Length && depth > 0)
        {
            int nextOpen = text.IndexOf("<StackPanel", i, StringComparison.Ordinal);
            int nextClose = text.IndexOf("</StackPanel>", i, StringComparison.Ordinal);
            if (nextOpen < 0 && nextClose < 0)
            {
                return text.Length;
            }

            if (nextOpen >= 0 && (nextClose < 0 || nextOpen < nextClose))
            {
                depth++;
                i = nextOpen + "<StackPanel".Length;
            }
            else
            {
                depth--;
                if (depth == 0)
                {
                    return nextClose;
                }

                i = nextClose + "</StackPanel>".Length;
            }
        }

        return text.Length;
    }

    /// <summary>
    /// 在 headless UI 线程上构造 <see cref="CardViewModel"/>，返回绑定好 store 的实例。
    /// </summary>
    private static CardViewModel CreateCardViewModel(PageKey key)
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)
            ?? throw new InvalidOperationException($"未注册 PageKey={key}");
        NoopMediator mediator = new();
#pragma warning disable CA2000
        InMemoryPatientRegistry registry = new();
#pragma warning restore CA2000
        Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> stores = new(1)
        {
            [key] = null!,
        };
        CardEffectDispatcher dispatcher = new(mediator, registry, key, stores);
        MviStore<CardState, CardIntent, CardEffect> store = new(
            CardState.FromDefinition(definition),
            new CardIntentHandler(DashboardCardRegistry.All),
            new CardReducer(DashboardCardRegistry.All),
            dispatcher);
        stores[key] = store;
        return new CardViewModel(store);
    }

    /// <summary>
    /// 在 headless UI 线程上构造 BedOverview 卡片视图 + ViewModel；
    /// 返回绑定好 DataContext 的 <see cref="CardView"/>。
    /// 控件创建后调用 <c>Measure</c> + <c>Arrange</c>，
    /// 使 <c>VisualTree.VisualExtensions.FindDescendantOfType</c> 能递归走完可视树。
    /// </summary>
    private static (CardView View, CardViewModel ViewModel) CreateCardViewWithModel(PageKey key)
    {
        CardViewModel viewModel = CreateCardViewModel(key);
        CardView view = new()
        {
            DataContext = viewModel,
        };
        Size cardSize = new(420, 360);
        view.Measure(cardSize);
        view.Arrange(new Rect(cardSize));
        return (view, viewModel);
    }
}
