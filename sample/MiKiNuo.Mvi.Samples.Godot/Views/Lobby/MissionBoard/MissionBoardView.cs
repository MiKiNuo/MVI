using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.MissionBoard;

/// <summary>
/// 表示任务大厅 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 编译期注册到
/// <see cref="IGodotMviViewRegistry"/>，供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过
/// <see cref="GodotMviViewRegistryAdapter"/> 按 <c>MissionBoardViewModel</c> 类型名解析并挂载。
/// </para>
/// </summary>
[MviGodotView("MissionBoardView", "res://Views/Lobby/MissionBoard/MissionBoardView.tscn")]
public partial class MissionBoardView : GodotMviControlView<MissionBoardViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(MissionBoardViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label missionLabel = GetNode<Label>("Root/Panel/Margin/Layout/MissionLabel");
        Label resourceLabel = GetNode<Label>("Root/Panel/Margin/Layout/ResourceLabel");
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/ForestButton"), viewModel.AcceptForestMissionCommand, bindings);
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/MineButton"), viewModel.AcceptMineMissionCommand, bindings);
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/CompleteButton"), viewModel.CompleteMissionCommand, bindings);
        BindPropertyChanged(viewModel, nameof(MissionBoardViewModel.SelectedMission), () => missionLabel.Text = $"当前任务：{viewModel.SelectedMission}\n{viewModel.MissionProgress}", bindings);
        BindPropertyChanged(viewModel, nameof(MissionBoardViewModel.MissionProgress), () => missionLabel.Text = $"当前任务：{viewModel.SelectedMission}\n{viewModel.MissionProgress}", bindings);
        BindPropertyChanged(viewModel, nameof(MissionBoardViewModel.Gold), () => resourceLabel.Text = $"金币：{viewModel.Gold}    体力：{viewModel.Stamina}", bindings);
        BindPropertyChanged(viewModel, nameof(MissionBoardViewModel.Stamina), () => resourceLabel.Text = $"金币：{viewModel.Gold}    体力：{viewModel.Stamina}", bindings);
    }
}
