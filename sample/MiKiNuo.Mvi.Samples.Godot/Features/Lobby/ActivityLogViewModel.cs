using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志 ViewModel。
/// </summary>
public sealed partial class ActivityLogViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化活动日志 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public ActivityLogViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>获取活动日志。</summary>
    [MviBind(nameof(LobbyState.ActivityLog))]
    public partial string ActivityLog { get; private set; }
}
