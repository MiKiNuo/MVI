using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面变更规约器。
/// </summary>
public sealed partial class OutpatientWorkstationMutationReducer
    : MviMutationReducerBase<OutpatientWorkstationState, OutpatientWorkstationMutation, OutpatientWorkstationEffect>
{
    /// <summary>
    /// 将变更应用到当前状态。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<OutpatientWorkstationState, OutpatientWorkstationEffect> Reduce(
        OutpatientWorkstationState state,
        OutpatientWorkstationMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<OutpatientWorkstationState, OutpatientWorkstationEffect>(state);
    }
}
