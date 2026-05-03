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
    /// <param name="state">当前状态。</param>
    /// <param name="intent">刷新页面意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> Reduce(
        BusinessCompositePageState state,
        BusinessCompositePageIntent.RefreshPage intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state);
    }

    /// <summary>
    /// 处理父页面业务上下文更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">上下文更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> Reduce(
        BusinessCompositePageState state,
        BusinessCompositePageIntent.UpdateContext intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state with
        {
            ActiveContext = intent.ActiveContext,
            FlowStatus = intent.FlowStatus
        });
    }

    /// <summary>
    /// 处理追加组合式 MVI 交互日志意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">追加日志意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> Reduce(
        BusinessCompositePageState state,
        BusinessCompositePageIntent.AppendInteractionLog intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        string nextLog = string.IsNullOrWhiteSpace(state.InteractionLog)
            ? intent.Message
            : $"{intent.Message}\n{state.InteractionLog}";

        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state with
        {
            InteractionLog = nextLog
        });
    }
}
