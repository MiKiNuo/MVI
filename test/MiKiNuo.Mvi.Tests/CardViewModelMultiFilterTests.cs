using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 CardViewModel 多选筛选属性与命令的单元测试。
/// 验证：
/// 1. AvailableBedTypes / AvailableBedStatuses 集合提供 4 个 BedType / 4 个 BedStatus。
/// 2. SelectedBedTypes / SelectedBedStatuses 与 state 同步。
/// 3. 派发 ToggleBedType/ToggleBedStatus 后 FilteredBeds 与 BedCatalog.Query 命中数一致。
/// </summary>
public sealed class CardViewModelMultiFilterTests
{
    /// <summary>
    /// BedOverview 卡片：AvailableBedTypes 暴露 4 种 BedType，每项含 DisplayName。
    /// </summary>
    [Test]
    public async Task BedOverview_AvailableBedTypes_ProvidesFourTypesWithDisplayAsync()
    {
        using CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);

        IReadOnlyList<BedTypeOption> options = viewModel.AvailableBedTypes;
        await Assert.That(options.Count).IsEqualTo(4);
        foreach (BedTypeOption option in options)
        {
            await Assert.That(option.Value).IsDefined();
            await Assert.That(option.DisplayName).IsNotNull().And.IsNotEmpty();
        }
    }

    /// <summary>
    /// BedOverview 卡片：AvailableBedStatuses 暴露 4 种 BedStatus。
    /// </summary>
    [Test]
    public async Task BedOverview_AvailableBedStatuses_ProvidesFourStatusesAsync()
    {
        using CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);

        IReadOnlyList<BedStatusOption> options = viewModel.AvailableBedStatuses;
        await Assert.That(options.Count).IsEqualTo(4);
    }

    /// <summary>
    /// 非 BedOverview 卡片：AvailableBedTypes/Statuses 仍提供完整集合（数据源恒定）。
    /// </summary>
    [Test]
    public async Task NonBedOverview_AvailableBedCollections_StillProvidedForUiTemplatingAsync()
    {
        using CardViewModel viewModel = CreateCardViewModel(PageKey.NursingTaskBoard);

        await Assert.That(viewModel.AvailableBedTypes.Count).IsEqualTo(4);
        await Assert.That(viewModel.AvailableBedStatuses.Count).IsEqualTo(4);
    }

    /// <summary>
    /// ToggleBedType 派发后，state.SelectedBedTypes 包含目标值，FilteredBeds 数量与 BedCatalog.Query 一致。
    /// </summary>
    [Test]
    public async Task ToggleBedType_DispatchAsync_FiltersBedsAsync()
    {
        using CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);

        await viewModel.ToggleBedTypeAsync(BedType.IntensiveCare, true);

        await Assert.That(viewModel.SelectedBedTypes.Contains(BedType.IntensiveCare)).IsTrue();
        int expected = BedCatalog.Query(viewModel.SelectedBedTypes, viewModel.SelectedBedStatuses).Count;
        await Assert.That(viewModel.FilteredBeds.Count).IsEqualTo(expected);
        await Assert.That(viewModel.FilteredBedRows.Count).IsEqualTo(expected);
    }

    /// <summary>
    /// ToggleBedStatus 派发后，state.SelectedBedStatuses 包含目标值。
    /// </summary>
    [Test]
    public async Task ToggleBedStatus_DispatchAsync_FiltersBedsAsync()
    {
        using CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);

        await viewModel.ToggleBedStatusAsync(BedStatus.Occupied, true);

        await Assert.That(viewModel.SelectedBedStatuses.Contains(BedStatus.Occupied)).IsTrue();
        int expected = BedCatalog.Query(viewModel.SelectedBedTypes, viewModel.SelectedBedStatuses).Count;
        await Assert.That(viewModel.FilteredBeds.Count).IsEqualTo(expected);
    }

    /// <summary>
    /// BedRecordRow 同样按多维筛选结果展示，且与 FilteredBeds 数量一致。
    /// </summary>
    [Test]
    public async Task MultiFilter_FilteredBedRows_MatchesFilteredBedsCountAsync()
    {
        using CardViewModel viewModel = CreateCardViewModel(PageKey.BedOverview);

        await viewModel.ToggleBedTypeAsync(BedType.General, true);
        await viewModel.ToggleBedStatusAsync(BedStatus.Occupied, true);

        int expected = BedCatalog.Query(viewModel.SelectedBedTypes, viewModel.SelectedBedStatuses).Count;
        await Assert.That(viewModel.FilteredBeds.Count).IsEqualTo(expected);
        await Assert.That(viewModel.FilteredBedRows.Count).IsEqualTo(expected);
        await Assert.That(viewModel.FilteredBedCount).IsEqualTo(expected);

        // 全部行都是 General + Occupied
        foreach (BedRecordRow row in viewModel.FilteredBedRows)
        {
            await Assert.That(row.Source.Type).IsEqualTo(BedType.General);
            await Assert.That(row.Source.Status).IsEqualTo(BedStatus.Occupied);
        }
    }

    private static CardViewModel CreateCardViewModel(PageKey key)
    {
#pragma warning disable CA2000
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)
            ?? throw new InvalidOperationException($"未注册 PageKey={key}");
        NoopMediator mediator = new();
        InMemoryPatientRegistry registry = new();
        Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> stores = new(1)
        {
            [key] = null!,
        };
        CardEffectDispatcher dispatcher = new(mediator, registry, key, stores);
        MviStore<CardState, CardIntent, CardEffect> store = new(
            CardState.FromDefinition(definition),
            new CardReducer(),
            dispatcher);
        stores[key] = store;
        return new CardViewModel(store);
#pragma warning restore CA2000
    }

    private sealed class NoopMediator : IMviMediator
    {
        /// <summary>
        /// 同步测试桩实现：始终返回 <c>default</c>，不真正路由请求。
        /// </summary>
        /// <typeparam name="TRequest">请求类型（未使用）。</typeparam>
        /// <typeparam name="TResponse">响应类型（未使用）。</typeparam>
        /// <param name="request">请求对象（未使用）。</param>
        /// <param name="cancellationToken">取消标记（未使用）。</param>
        /// <returns>默认响应。</returns>
        public ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : notnull
        {
            return ValueTask.FromResult<TResponse>(default!);
        }
    }
}
