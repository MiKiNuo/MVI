using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Menu;

/// <summary>
/// 表示大厅菜单 View。
/// </summary>
public partial class LobbyMenuView : GodotMviControlView<LobbyMenuViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(LobbyMenuViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Button missionButton = GetNode<Button>("Root/Panel/Margin/Layout/MissionButton");
        Button heroButton = GetNode<Button>("Root/Panel/Margin/Layout/HeroButton");
        Button inventoryButton = GetNode<Button>("Root/Panel/Margin/Layout/InventoryButton");
        Button forgeButton = GetNode<Button>("Root/Panel/Margin/Layout/ForgeButton");
        Button battleButton = GetNode<Button>("Root/Panel/Margin/Layout/BattleButton");
        Button logoutButton = GetNode<Button>("Root/Panel/Margin/Layout/LogoutButton");
        BindButton(missionButton, viewModel.SelectMissionBoardCommand, bindings);
        BindButton(heroButton, viewModel.SelectHeroRosterCommand, bindings);
        BindButton(inventoryButton, viewModel.SelectInventoryCommand, bindings);
        BindButton(forgeButton, viewModel.SelectForgeLabCommand, bindings);
        BindButton(battleButton, viewModel.SelectBattlePrepCommand, bindings);
        BindButton(logoutButton, viewModel.LogoutCommand, bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => missionButton.Text = Mark(viewModel.CurrentPanel, LobbyPanelKeys.MissionBoard, "任务大厅"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => heroButton.Text = Mark(viewModel.CurrentPanel, LobbyPanelKeys.HeroRoster, "英雄队伍"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => inventoryButton.Text = Mark(viewModel.CurrentPanel, LobbyPanelKeys.Inventory, "背包仓库"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => forgeButton.Text = Mark(viewModel.CurrentPanel, LobbyPanelKeys.ForgeLab, "锻造工坊"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => battleButton.Text = Mark(viewModel.CurrentPanel, LobbyPanelKeys.BattlePrep, "战斗准备"), bindings);
    }

    private static string Mark(string currentPanel, string panel, string text)
    {
        return string.Equals(currentPanel, panel, StringComparison.Ordinal) ? $"✓ {text}" : text;
    }
}
