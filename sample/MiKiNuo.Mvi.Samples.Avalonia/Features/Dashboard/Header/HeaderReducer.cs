using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件规约器。
/// </summary>
public sealed partial class HeaderReducer
    : MviReducerBase<HeaderState, HeaderIntent, UnitEffect>
{
    /// <summary>
    /// 处理更新标题意图。
    /// </summary>
    [MviReduce(typeof(HeaderIntent.UpdateTitle))]
    private MviReduceResult<HeaderState, UnitEffect> HandleUpdateTitle(
        HeaderState state,
        HeaderIntent.UpdateTitle intent,
        IMviBusinessResult? result)
    {
        return MviReduceResult.State<HeaderState, UnitEffect>(
            state with { Title = intent.Title, SubTitle = intent.SubTitle });
    }
}
