using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeaturesCards;

/// <summary>
/// 表示业务指标卡片意图。
/// </summary>
public abstract partial record MetricCardIntent : IMviIntent
{
    /// <summary>
    /// 表示刷新卡片意图。
    /// </summary>
    public sealed partial record Refresh : MetricCardIntent;
}
