using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeaturesCards;

/// <summary>
/// 表示业务指标卡片状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="Value">指标值。</param>
/// <param name="Status">状态。</param>
/// <param name="Detail">详情。</param>
/// <param name="CanRefresh">是否可以刷新。</param>
public sealed record MetricCardState(
    string Title,
    string Value,
    string Status,
    string Detail,
    bool CanRefresh) : IMviState;
