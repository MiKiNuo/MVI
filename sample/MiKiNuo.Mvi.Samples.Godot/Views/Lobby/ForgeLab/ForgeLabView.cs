using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.ForgeLab;

/// <summary>
/// 表示锻造工坊 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 编译期注册到
/// <see cref="IGodotMviViewRegistry"/>，供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过
/// <see cref="GodotMviViewRegistryAdapter"/> 按 <c>ForgeLabViewModel</c> 类型名解析并挂载。
/// </para>
/// </summary>
[MviGodotView("ForgeLabView", "res://Views/Lobby/ForgeLab/ForgeLabView.tscn")]
public partial class ForgeLabView : GodotMviControlView<ForgeLabViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(ForgeLabViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label forgeLabel = GetNode<Label>("Root/Panel/Margin/Layout/ForgeLabel");
        Label reuseLabel = GetNode<Label>("Root/Panel/Margin/Layout/ReuseLabel");
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/WeaponButton"), viewModel.ForgeWeaponCommand, bindings);
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/ArmorButton"), viewModel.ForgeArmorCommand, bindings);
        Action update = () => forgeLabel.Text = $"锻造评分：{viewModel.ForgeScore}\n矿石：{viewModel.OreCount}    水晶：{viewModel.CrystalCount}    队伍战力：{viewModel.HeroTeamPower}";
        BindPropertyChanged(viewModel, nameof(ForgeLabViewModel.ForgeScore), update, bindings);
        BindPropertyChanged(viewModel, nameof(ForgeLabViewModel.OreCount), update, bindings);
        BindPropertyChanged(viewModel, nameof(ForgeLabViewModel.CrystalCount), update, bindings);
        BindPropertyChanged(viewModel, nameof(ForgeLabViewModel.HeroTeamPower), update, bindings);
        reuseLabel.Text = "逻辑复用验证：锻造评分、任务奖励、英雄训练成本都来自同一个 GameLogicService，而不是写在 ViewModel 或 View 中。";
    }
}
