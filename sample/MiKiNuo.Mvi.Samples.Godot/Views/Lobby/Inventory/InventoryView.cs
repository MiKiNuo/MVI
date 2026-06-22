using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Inventory;

/// <summary>
/// 表示背包仓库 View。
/// <para>
/// 标有 <see cref="MviGodotViewAttribute"/>：由 <c>GodotMviViewRegistryGenerator</c> 编译期注册到
/// <see cref="IGodotMviViewRegistry"/>，供父 <c>LobbyView</c> 的 <c>[MviSlot]</c> 槽位通过
/// <see cref="GodotMviViewRegistryAdapter"/> 按 <c>InventoryViewModel</c> 类型名解析并挂载。
/// </para>
/// </summary>
[MviGodotView("InventoryView", "res://Views/Lobby/Inventory/InventoryView.tscn")]
public partial class InventoryView : GodotMviControlView<InventoryViewModel>
{
    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(InventoryViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);
        Label itemsLabel = GetNode<Label>("Root/Panel/Margin/Layout/ItemsLabel");
        Label resourceLabel = GetNode<Label>("Root/Panel/Margin/Layout/ResourceLabel");
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/PotionButton"), viewModel.UsePotionCommand, bindings);
        BindButton(GetNode<Button>("Root/Panel/Margin/Layout/Buttons/GoldBoxButton"), viewModel.OpenGoldBoxCommand, bindings);
        Action updateItems = () => itemsLabel.Text = $"药水：{viewModel.PotionCount}    矿石：{viewModel.OreCount}    水晶：{viewModel.CrystalCount}";
        BindPropertyChanged(viewModel, nameof(InventoryViewModel.PotionCount), updateItems, bindings);
        BindPropertyChanged(viewModel, nameof(InventoryViewModel.OreCount), updateItems, bindings);
        BindPropertyChanged(viewModel, nameof(InventoryViewModel.CrystalCount), updateItems, bindings);
        BindPropertyChanged(viewModel, nameof(InventoryViewModel.Gold), () => resourceLabel.Text = $"金币：{viewModel.Gold}    体力：{viewModel.Stamina}", bindings);
        BindPropertyChanged(viewModel, nameof(InventoryViewModel.Stamina), () => resourceLabel.Text = $"金币：{viewModel.Gold}    体力：{viewModel.Stamina}", bindings);
    }
}
