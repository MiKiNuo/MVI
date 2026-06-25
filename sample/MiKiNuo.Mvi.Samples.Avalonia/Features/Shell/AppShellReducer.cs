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
    [MviReduce(typeof(AppShellIntent.ShowPage))]
    private static MviReduceResult<AppShellState, AppShellEffect> HandleShowPage(
        AppShellState state,
        AppShellIntent.ShowPage intent)
    {
        return MviReduceResult.State<AppShellState, AppShellEffect>(
            state with { CurrentPageKey = intent.PageKey, Title = intent.Title });
    }
}
