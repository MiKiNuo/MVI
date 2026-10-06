namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
/// <summary>壳的纯状态转换。</summary>
public sealed partial class AppShellReducer : MviReducerBase<AppShellState>
{
    [MviReduce(typeof(AppShellMutation))]
    private static AppShellState Change(AppShellState state, AppShellMutation change)
        => new(change.Page, change.Page == ShellPage.Home ? change.DisplayName : null);
}
