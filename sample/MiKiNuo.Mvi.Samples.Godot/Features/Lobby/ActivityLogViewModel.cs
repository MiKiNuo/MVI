using MiKiNuo.Mvi.Application.MVI.Store;
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
    public ActivityLogViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store)
        : base(store)
    {
    }

    /// <summary>获取活动日志。</summary>
    [MviBind(nameof(LobbyState.ActivityLog))]
    public partial string ActivityLog { get; private set; }
}
