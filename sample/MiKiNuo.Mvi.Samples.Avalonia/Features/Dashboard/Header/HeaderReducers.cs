using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件规约器。
/// </summary>
public sealed partial class HeaderReducer
    : MviReducerBase<HeaderState, HeaderIntent, HeaderEffect>
{
    /// <summary>
    /// 处理更新标题意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">更新标题意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<HeaderState, HeaderEffect> Reduce(
        HeaderState state,
        HeaderIntent.UpdateTitle intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<HeaderState, HeaderEffect>(state with
        {
            Title = intent.Title,
            SubTitle = intent.SubTitle
        });
    }
}
