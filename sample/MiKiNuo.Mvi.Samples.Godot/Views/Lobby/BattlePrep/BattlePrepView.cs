using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.BattlePrep;

/// <summary>
/// 表示战斗准备 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 编译期注册到
/// <see cref="IGodotMviViewRegistry"/>，供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过
/// <see cref="GodotMviViewRegistryAdapter"/> 按 <c>BattlePrepViewModel</c> 类型名解析并挂载。
/// </para>
/// </summary>
[MviGodotView("BattlePrepView", "res://Views/Lobby/BattlePrep/BattlePrepView.tscn")]
public partial class BattlePrepView : GodotMviControlView<BattlePrepViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(BattlePrepViewModel viewModel, MviDisposableBag bindings)
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
