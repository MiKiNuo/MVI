using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeaturesCards;

/// <summary>
/// 表示业务指标卡片规约器。
/// </summary>
public static class MetricCardReducers
{
    /// <summary>
    /// 处理刷新卡片意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">刷新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<MetricCardState, MetricCardEffect> Reduce(
        MetricCardState state,
        MetricCardIntent.Refresh intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<MetricCardState, MetricCardEffect>(state with
        {
            Status = "已刷新",
            Detail = $"{state.Detail} · 最新刷新 {DateTime.Now:HH:mm:ss}"
        });
    }
}
