using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面规约器。
/// </summary>
public sealed class OutpatientWorkstationReducer
    : MviReducerBase<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<OutpatientWorkstationState, OutpatientWorkstationEffect> Reduce(
        OutpatientWorkstationState state,
        OutpatientWorkstationIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<OutpatientWorkstationState, OutpatientWorkstationEffect>(state);
    }
}
