using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件规约器。
/// </summary>
public sealed class HeaderReducer
    : MviReducerBase<HeaderState, HeaderIntent, HeaderEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<HeaderState, HeaderEffect> Reduce(
        HeaderState state,
        HeaderIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            HeaderIntent.UpdateTitle updateTitle => MviReduceResult.State<HeaderState, HeaderEffect>(
                state with { Title = updateTitle.Title, SubTitle = updateTitle.SubTitle }),
            _ => MviReduceResult.State<HeaderState, HeaderEffect>(state),
        };
    }
}
