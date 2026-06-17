using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.HeroRoster;

/// <summary>
/// 表示英雄队伍 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 编译期注册到
/// <see cref="IGodotMviViewRegistry"/>，供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过
/// <see cref="GodotMviViewRegistryAdapter"/> 按 <c>HeroRosterViewModel</c> 类型名解析并挂载。
/// </para>
/// </summary>
[MviGodotView("HeroRosterView", "res://Views/Lobby/HeroRoster/HeroRosterView.tscn")]
public partial class HeroRosterView : GodotMviControlView<HeroRosterViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(HeroRosterViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label heroLabel = GetNode<Label>("Root/Panel/Margin/Layout/HeroLabel");
        Label goldLabel = GetNode<Label>("Root/Panel/Margin/Layout/GoldLabel");
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/WarriorButton"), viewModel.TrainWarriorCommand, bindings);
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/MageButton"), viewModel.TrainMageCommand, bindings);
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/ArcherButton"), viewModel.TrainArcherCommand, bindings);
        Action updateHero = () => heroLabel.Text = $"战士 Lv.{viewModel.WarriorLevel}    法师 Lv.{viewModel.MageLevel}    弓手 Lv.{viewModel.ArcherLevel}\n队伍战力：{viewModel.HeroTeamPower}";
        BindPropertyChanged(viewModel, nameof(HeroRosterViewModel.WarriorLevel), updateHero, bindings);
        BindPropertyChanged(viewModel, nameof(HeroRosterViewModel.MageLevel), updateHero, bindings);
        BindPropertyChanged(viewModel, nameof(HeroRosterViewModel.ArcherLevel), updateHero, bindings);
        BindPropertyChanged(viewModel, nameof(HeroRosterViewModel.HeroTeamPower), updateHero, bindings);
        BindPropertyChanged(viewModel, nameof(HeroRosterViewModel.Gold), () => goldLabel.Text = $"金币：{viewModel.Gold}；训练消耗由 GameLogicService 统一计算。", bindings);
    }
}
