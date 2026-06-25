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
    [MviReduce(typeof(HeaderIntent.UpdateTitle))]
    private MviReduceResult<HeaderState, HeaderEffect> HandleUpdateTitle(
        HeaderState state,
        HeaderIntent.UpdateTitle intent)
    {
        return MviReduceResult.State<HeaderState, HeaderEffect>(
            state with { Title = intent.Title, SubTitle = intent.SubTitle });
    }
}
