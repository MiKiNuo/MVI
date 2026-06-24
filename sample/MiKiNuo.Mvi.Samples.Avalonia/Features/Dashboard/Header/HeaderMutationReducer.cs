using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件变更规约器。
/// </summary>
public sealed partial class HeaderMutationReducer
    : MviMutationReducerBase<HeaderState, HeaderMutation, HeaderEffect>
{
    /// <summary>
    /// 应用设置标题变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<HeaderState, HeaderEffect> HandleSetTitle(
        HeaderState state,
        HeaderMutation.SetTitle mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<HeaderState, HeaderEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置副标题变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<HeaderState, HeaderEffect> HandleSetSubTitle(
        HeaderState state,
        HeaderMutation.SetSubTitle mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<HeaderState, HeaderEffect>(state.Apply(mutation));
    }
}
