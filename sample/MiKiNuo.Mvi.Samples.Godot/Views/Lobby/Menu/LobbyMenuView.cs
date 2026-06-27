using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Menu;

/// <summary>
/// 表示大厅菜单 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 编译期注册到
/// <see cref="IGodotMviViewRegistry"/>，供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过
/// <see cref="GodotMviViewRegistryAdapter"/> 按 <c>LobbyMenuViewModel</c> 类型名解析并挂载。
/// </para>
/// </summary>
[MviGodotView("LobbyMenuView", "res://Views/Lobby/Menu/LobbyMenuView.tscn")]
public partial class LobbyMenuView : GodotMviControlView<LobbyMenuViewModel>
{
    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
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
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => missionButton.Text = Mark(viewModel.CurrentPanel, LobbyPanel.MissionBoard, "任务大厅"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => heroButton.Text = Mark(viewModel.CurrentPanel, LobbyPanel.HeroRoster, "英雄队伍"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => inventoryButton.Text = Mark(viewModel.CurrentPanel, LobbyPanel.Inventory, "背包仓库"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => forgeButton.Text = Mark(viewModel.CurrentPanel, LobbyPanel.ForgeLab, "锻造工坊"), bindings);
        BindPropertyChanged(viewModel, nameof(LobbyMenuViewModel.CurrentPanel), () => battleButton.Text = Mark(viewModel.CurrentPanel, LobbyPanel.BattlePrep, "战斗准备"), bindings);
    }

    private static string Mark(LobbyPanel currentPanel, LobbyPanel panel, string text)
    {
        return currentPanel == panel ? $"✓ {text}" : text;
    }
}
