using System;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.BattlePrep;

/// <summary>
/// 表示战斗准备 View。
/// </summary>
public partial class BattlePrepView : GodotMviControlView<BattlePrepViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(BattlePrepViewModel viewModel, GodotMviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label battleLabel = GetNode<Label>("Root/Panel/Margin/Layout/BattleLabel");
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/PrepareButton"), viewModel.PrepareBattleCommand, bindings);
        Action update = () => battleLabel.Text = $"当前任务：{viewModel.SelectedMission}\n战力：{viewModel.HeroTeamPower}    体力：{viewModel.Stamina}\n{viewModel.BattleReadyText}";
        BindPropertyChanged(viewModel, nameof(BattlePrepViewModel.SelectedMission), update, bindings);
        BindPropertyChanged(viewModel, nameof(BattlePrepViewModel.HeroTeamPower), update, bindings);
        BindPropertyChanged(viewModel, nameof(BattlePrepViewModel.Stamina), update, bindings);
        BindPropertyChanged(viewModel, nameof(BattlePrepViewModel.BattleReadyText), update, bindings);
    }
}
