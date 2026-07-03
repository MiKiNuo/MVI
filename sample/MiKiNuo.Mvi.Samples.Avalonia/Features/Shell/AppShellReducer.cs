using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳规约器。
/// </summary>
public sealed partial class AppShellReducer
    : MviReducerBase<AppShellState, AppShellIntent, UnitEffect>
{
    /// <summary>
    /// 处理显示页面意图。
    /// </summary>
    [MviReduce(typeof(AppShellIntent.ShowPage))]
    private MviReduceResult<AppShellState, UnitEffect> HandleShowPage(
        AppShellState state,
        AppShellIntent.ShowPage intent,
        IMviBusinessResult? result)
    {
        return Unchanged(
            state with { CurrentPageKey = intent.PageKey, Title = intent.Title });
    }
}
