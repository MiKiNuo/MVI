using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面规约器。
/// </summary>
public sealed partial class OutpatientWorkstationReducer
    : MviReducerBase<OutpatientWorkstationState, OutpatientWorkstationIntent, UnitEffect>
{
    /// <summary>
    /// 处理刷新页面布局意图。
    /// </summary>
    [MviReduce(typeof(OutpatientWorkstationIntent.RefreshPage))]
    private MviReduceResult<OutpatientWorkstationState, UnitEffect> HandleRefreshPage(
        OutpatientWorkstationState state,
        OutpatientWorkstationIntent.RefreshPage intent,
        IMviBusinessResult? result)
    {
        return Unchanged(state);
    }
}
