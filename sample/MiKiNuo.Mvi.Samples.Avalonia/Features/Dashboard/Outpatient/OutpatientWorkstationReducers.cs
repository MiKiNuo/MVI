using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面规约器。
/// </summary>
public static class OutpatientWorkstationReducers
{
    /// <summary>
    /// 处理刷新页面布局意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">刷新页面意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<OutpatientWorkstationState, OutpatientWorkstationEffect> Reduce(
        OutpatientWorkstationState state,
        OutpatientWorkstationIntent.RefreshPage intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<OutpatientWorkstationState, OutpatientWorkstationEffect>(state);
    }
}
