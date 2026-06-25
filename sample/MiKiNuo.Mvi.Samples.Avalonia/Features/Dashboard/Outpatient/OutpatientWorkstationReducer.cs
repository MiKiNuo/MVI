using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面规约器。
/// </summary>
public sealed partial class OutpatientWorkstationReducer
    : MviReducerBase<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect>
{
    /// <summary>
    /// 处理刷新页面布局意图。
    /// </summary>
    [MviReduce(typeof(OutpatientWorkstationIntent.RefreshPage))]
    private MviReduceResult<OutpatientWorkstationState, OutpatientWorkstationEffect> HandleRefreshPage(
        OutpatientWorkstationState state,
        OutpatientWorkstationIntent.RefreshPage intent)
    {
        return MviReduceResult.State<OutpatientWorkstationState, OutpatientWorkstationEffect>(state);
    }
}
