using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳规约器。
/// </summary>
public sealed class AppShellReducer
    : MviReducerBase<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<AppShellState, AppShellEffect> Reduce(
        AppShellState state,
        AppShellIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            AppShellIntent.ShowPage showPage => MviReduceResult.State<AppShellState, AppShellEffect>(
                state with { CurrentPageKey = showPage.PageKey, Title = showPage.Title }),
            _ => MviReduceResult.State<AppShellState, AppShellEffect>(state),
        };
    }
}
