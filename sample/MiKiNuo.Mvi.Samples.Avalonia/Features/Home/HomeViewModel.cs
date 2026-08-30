using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页 ViewModel。用户显示名经 <see cref="MviViewModelBase{TState, TIntent, TEffect}.BindSiblingState"/> 来自应用壳。
/// </summary>
public sealed partial class HomeViewModel
    : MviViewModelBase<HomeState, HomeIntent, HomeEffect>
{
    private string _displayName = string.Empty;

    /// <summary>
    /// 初始化主页 ViewModel。
    /// </summary>
    /// <param name="store">主页状态存储。</param>
    /// <param name="shellStore">应用壳状态存储（兄弟 Store，只读订阅）。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public HomeViewModel(
        IMviStore<HomeState, HomeIntent, HomeEffect> store,
        IMviStore<AppShellState, AppShellIntent, UnitEffect> shellStore,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(shellStore);
        _ = BindSiblingState(shellStore, ApplyShellState);
        ApplyShellState(shellStore.CurrentState);
    }

    /// <summary>
    /// 获取已登录用户显示名。
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    /// <summary>
    /// 获取退出登录命令。
    /// </summary>
    [MviCommand(typeof(HomeIntent.Logout))]
    public partial IMviAsyncCommand LogoutCommand { get; private set; }

    private void ApplyShellState(AppShellState shellState)
    {
        DisplayName = shellState.DisplayName ?? string.Empty;
    }
}
