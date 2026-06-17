using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.PlayerHeader;

/// <summary>
/// 表示玩家头部状态 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 在编译期
/// 把本 View（连同 <c>.tscn</c> 根节点）注册到 <see cref="IGodotMviViewRegistry"/>，
/// 供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过 <see cref="GodotMviViewRegistryAdapter"/>
/// 按 <c>PlayerHeaderViewModel</c> 类型名解析为 <c>PlayerHeaderView</c> 注册键并自动挂载。
/// </para>
/// </summary>
[MviGodotView("PlayerHeaderView", "res://Views/Lobby/PlayerHeader/PlayerHeaderView.tscn")]
public partial class PlayerHeaderView : GodotMviControlView<PlayerHeaderViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(PlayerHeaderViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label infoLabel = GetNode<Label>("Root/Panel/Margin/Layout/InfoLabel");
        Label resourceLabel = GetNode<Label>("Root/Panel/Margin/Layout/ResourceLabel");
        Label panelLabel = GetNode<Label>("Root/Panel/Margin/Layout/PanelLabel");
        BindPropertyChanged(viewModel, nameof(PlayerHeaderViewModel.PlayerName), () => infoLabel.Text = $"{viewModel.PlayerName}  Lv.{viewModel.PlayerLevel}", bindings);
        BindPropertyChanged(viewModel, nameof(PlayerHeaderViewModel.PlayerLevel), () => infoLabel.Text = $"{viewModel.PlayerName}  Lv.{viewModel.PlayerLevel}", bindings);
        BindPropertyChanged(viewModel, nameof(PlayerHeaderViewModel.Gold), () => resourceLabel.Text = $"金币：{viewModel.Gold}    体力：{viewModel.Stamina}", bindings);
        BindPropertyChanged(viewModel, nameof(PlayerHeaderViewModel.Stamina), () => resourceLabel.Text = $"金币：{viewModel.Gold}    体力：{viewModel.Stamina}", bindings);
        BindPropertyChanged(viewModel, nameof(PlayerHeaderViewModel.CurrentPanelTitle), () => panelLabel.Text = $"当前功能：{viewModel.CurrentPanelTitle}", bindings);
    }
}
