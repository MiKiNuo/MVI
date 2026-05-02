using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeaturesCards;

/// <summary>
/// 表示业务指标卡片 ViewModel。
/// </summary>
public sealed partial class MetricCardViewModel
    : MviViewModelBase<MetricCardState, MetricCardIntent, MetricCardEffect>
{
    /// <summary>
    /// 初始化业务指标卡片 ViewModel。
    /// </summary>
    /// <param name="store">业务指标卡片状态存储。</param>
    public MetricCardViewModel(IMviStore<MetricCardState, MetricCardIntent, MetricCardEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(MetricCardState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取指标值。
    /// </summary>
    [MviBind(nameof(MetricCardState.Value))]
    public partial string Value { get; private set; }

    /// <summary>
    /// 获取状态。
    /// </summary>
    [MviBind(nameof(MetricCardState.Status))]
    public partial string Status { get; private set; }

    /// <summary>
    /// 获取详情。
    /// </summary>
    [MviBind(nameof(MetricCardState.Detail))]
    public partial string Detail { get; private set; }

    /// <summary>
    /// 获取是否可以刷新。
    /// </summary>
    [MviBind(nameof(MetricCardState.CanRefresh))]
    public partial bool CanRefresh { get; private set; }

    /// <summary>
    /// 获取刷新命令。
    /// </summary>
    [MviCommand(typeof(MetricCardIntent.Refresh), CanExecuteProperty = nameof(CanRefresh), IsAsync = true)]
    public partial IMviAsyncCommand RefreshCommand { get; private set; }
}
