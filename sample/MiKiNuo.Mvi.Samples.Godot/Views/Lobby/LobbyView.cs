using System;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ActivityLog;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.BattlePrep;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ForgeLab;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.HeroRoster;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Inventory;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Menu;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.MissionBoard;
using MiKiNuo.Mvi.Samples.Godot.Views.Lobby.PlayerHeader;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby;

/// <summary>
/// 表示 Godot 游戏大厅组合 View。
/// </summary>
public partial class LobbyView : GodotMviControlView<LobbyViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(LobbyViewModel viewModel, GodotMviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        PlayerHeaderView headerView = GetNode<PlayerHeaderView>("Root/PlayerHeaderView");
        LobbyMenuView menuView = GetNode<LobbyMenuView>("Root/Body/LobbyMenuView");
        MissionBoardView missionView = GetNode<MissionBoardView>("Root/Body/Content/MissionBoardView");
        HeroRosterView heroView = GetNode<HeroRosterView>("Root/Body/Content/HeroRosterView");
        InventoryView inventoryView = GetNode<InventoryView>("Root/Body/Content/InventoryView");
        ForgeLabView forgeView = GetNode<ForgeLabView>("Root/Body/Content/ForgeLabView");
        BattlePrepView battleView = GetNode<BattlePrepView>("Root/Body/Content/BattlePrepView");
        ActivityLogView logView = GetNode<ActivityLogView>("Root/ActivityLogView");

        if (viewModel.PlayerHeaderViewModel is not null) headerView.Bind(viewModel.PlayerHeaderViewModel);
        if (viewModel.LobbyMenuViewModel is not null) menuView.Bind(viewModel.LobbyMenuViewModel);
        if (viewModel.MissionBoardViewModel is not null) missionView.Bind(viewModel.MissionBoardViewModel);
        if (viewModel.HeroRosterViewModel is not null) heroView.Bind(viewModel.HeroRosterViewModel);
        if (viewModel.InventoryViewModel is not null) inventoryView.Bind(viewModel.InventoryViewModel);
        if (viewModel.ForgeLabViewModel is not null) forgeView.Bind(viewModel.ForgeLabViewModel);
        if (viewModel.BattlePrepViewModel is not null) battleView.Bind(viewModel.BattlePrepViewModel);
        if (viewModel.ActivityLogViewModel is not null) logView.Bind(viewModel.ActivityLogViewModel);

        BindPropertyChanged(
            viewModel,
            nameof(LobbyViewModel.CurrentPanel),
            () =>
            {
                missionView.Visible = string.Equals(viewModel.CurrentPanel, LobbyPanelKeys.MissionBoard, StringComparison.Ordinal);
                heroView.Visible = string.Equals(viewModel.CurrentPanel, LobbyPanelKeys.HeroRoster, StringComparison.Ordinal);
                inventoryView.Visible = string.Equals(viewModel.CurrentPanel, LobbyPanelKeys.Inventory, StringComparison.Ordinal);
                forgeView.Visible = string.Equals(viewModel.CurrentPanel, LobbyPanelKeys.ForgeLab, StringComparison.Ordinal);
                battleView.Visible = string.Equals(viewModel.CurrentPanel, LobbyPanelKeys.BattlePrep, StringComparison.Ordinal);
            },
            bindings);
    }
}
