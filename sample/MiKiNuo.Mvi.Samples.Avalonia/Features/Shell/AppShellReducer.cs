using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳规约器。
/// </summary>
/// <remarks>
/// 应用壳无副作用，Effect 通道使用 <see cref="UnitEffect"/>，
/// 容器装配时自动接入 NullEffectDispatcher。
/// </remarks>
[MviFeature]
public sealed partial class AppShellReducer
    : MviReducerBase<AppShellState, AppShellIntent, UnitEffect>
{
    /// <summary>
    /// 处理导航到登录页意图。
    /// </summary>
    [MviReduce(typeof(AppShellIntent.ShowLogin))]
    private MviReduceResult<AppShellState, UnitEffect> HandleShowLogin(
        AppShellState state,
        AppShellIntent.ShowLogin intent)
    {
        return Unchanged(state with { CurrentPage = ShellPage.Login, DisplayName = null });
    }

    /// <summary>
    /// 处理导航到注册页意图。
    /// </summary>
    [MviReduce(typeof(AppShellIntent.ShowRegister))]
    private MviReduceResult<AppShellState, UnitEffect> HandleShowRegister(
        AppShellState state,
        AppShellIntent.ShowRegister intent)
    {
        return Unchanged(state with { CurrentPage = ShellPage.Register });
    }

    /// <summary>
    /// 处理导航到主页意图。
    /// </summary>
    [MviReduce(typeof(AppShellIntent.ShowHome))]
    private MviReduceResult<AppShellState, UnitEffect> HandleShowHome(
        AppShellState state,
        AppShellIntent.ShowHome intent)
    {
        return Unchanged(state with { CurrentPage = ShellPage.Home, DisplayName = intent.DisplayName });
    }
}
