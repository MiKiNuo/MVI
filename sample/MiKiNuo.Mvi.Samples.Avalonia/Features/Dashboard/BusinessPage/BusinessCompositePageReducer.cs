using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面规约器。
/// </summary>
public sealed partial class BusinessCompositePageReducer
    : MviReducerBase<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect>
{
    /// <summary>
    /// 处理刷新页面布局意图。
    /// </summary>
    [MviReduce(typeof(BusinessCompositePageIntent.RefreshPage))]
    private static MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> HandleRefreshPage(
        BusinessCompositePageState state,
        BusinessCompositePageIntent.RefreshPage intent)
    {
        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state);
    }

    /// <summary>
    /// 处理更新上下文意图。
    /// </summary>
    [MviReduce(typeof(BusinessCompositePageIntent.UpdateContext))]
    private static MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> HandleUpdateContext(
        BusinessCompositePageState state,
        BusinessCompositePageIntent.UpdateContext intent)
    {
        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(
            state with { ActiveContext = intent.ActiveContext, FlowStatus = intent.FlowStatus });
    }

    /// <summary>
    /// 处理追加交互日志意图。
    /// </summary>
    [MviReduce(typeof(BusinessCompositePageIntent.AppendInteractionLog))]
    private static MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> HandleAppendInteractionLog(
        BusinessCompositePageState state,
        BusinessCompositePageIntent.AppendInteractionLog intent)
    {
        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(
            state with { InteractionLog = ComputeNextLog(state.InteractionLog, intent.Message) });
    }

    private static string ComputeNextLog(string currentLog, string message)
    {
        return string.IsNullOrWhiteSpace(currentLog)
            ? message
            : message + "\n" + currentLog;
    }
}
