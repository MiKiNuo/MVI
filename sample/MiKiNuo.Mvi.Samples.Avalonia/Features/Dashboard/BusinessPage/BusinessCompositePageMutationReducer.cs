using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面变更规约器。
/// </summary>
public sealed partial class BusinessCompositePageMutationReducer
    : MviMutationReducerBase<BusinessCompositePageState, BusinessCompositePageMutation, BusinessCompositePageEffect>
{
    /// <summary>
    /// 应用设置当前业务上下文变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> HandleSetActiveContext(
        BusinessCompositePageState state,
        BusinessCompositePageMutation.SetActiveContext mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置当前流程状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> HandleSetFlowStatus(
        BusinessCompositePageState state,
        BusinessCompositePageMutation.SetFlowStatus mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置交互日志变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> HandleSetInteractionLog(
        BusinessCompositePageState state,
        BusinessCompositePageMutation.SetInteractionLog mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state.Apply(mutation));
    }
}
