using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳规约器。
/// </summary>
public sealed partial class AppShellReducer
    : MviReducerBase<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>
    /// 处理显示页面意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">显示页面意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AppShellState, AppShellEffect> Reduce(
        AppShellState state,
        AppShellIntent.ShowPage intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        AppShellState nextState = state with
        {
            Title = intent.Title,
            CurrentViewModel = intent.ViewModel
        };

        return MviReduceResult.State<AppShellState, AppShellEffect>(nextState);
    }
}
