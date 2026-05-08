using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 ViewModel。
/// </summary>
public sealed partial class AppShellViewModel : MviViewModelBase<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>
    /// 初始化游戏应用壳 ViewModel。
    /// </summary>
    /// <param name="store">应用壳状态存储。</param>
    public AppShellViewModel(IMviStore<AppShellState, AppShellIntent, AppShellEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取当前顶层页面键。
    /// </summary>
    [MviBind(nameof(AppShellState.CurrentScreen))]
    public partial string CurrentScreen { get; private set; }

    /// <summary>
    /// 获取当前页面标题。
    /// </summary>
    [MviBind(nameof(AppShellState.CurrentTitle))]
    public partial string CurrentTitle { get; private set; }

    /// <summary>
    /// 获取应用壳提示消息。
    /// </summary>
    [MviBind(nameof(AppShellState.ShellMessage))]
    public partial string ShellMessage { get; private set; }

    /// <summary>
    /// 获取登录页 ViewModel。
    /// </summary>
    [MviBind(nameof(AppShellState.LoginViewModel))]
    public partial LoginViewModel? LoginViewModel { get; private set; }

    /// <summary>
    /// 获取游戏大厅 ViewModel。
    /// </summary>
    [MviBind(nameof(AppShellState.LobbyViewModel))]
    public partial LobbyViewModel? LobbyViewModel { get; private set; }
}
