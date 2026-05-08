using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家头部状态 ViewModel。
/// </summary>
public sealed partial class PlayerHeaderViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化玩家头部状态 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    public PlayerHeaderViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store)
        : base(store)
    {
    }

    /// <summary>获取玩家名称。</summary>
    [MviBind(nameof(LobbyState.PlayerName))]
    public partial string PlayerName { get; private set; }

    /// <summary>获取玩家等级。</summary>
    [MviBind(nameof(LobbyState.PlayerLevel))]
    public partial int PlayerLevel { get; private set; }

    /// <summary>获取金币数量。</summary>
    [MviBind(nameof(LobbyState.Gold))]
    public partial int Gold { get; private set; }

    /// <summary>获取体力值。</summary>
    [MviBind(nameof(LobbyState.Stamina))]
    public partial int Stamina { get; private set; }

    /// <summary>获取当前大厅面板标题。</summary>
    [MviBind(nameof(LobbyState.CurrentPanelTitle))]
    public partial string CurrentPanelTitle { get; private set; }
}
