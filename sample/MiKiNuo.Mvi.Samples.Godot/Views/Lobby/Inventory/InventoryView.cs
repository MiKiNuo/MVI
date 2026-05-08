using System;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Views.Lobby.Inventory;

/// <summary>
/// 表示背包仓库 View。
/// </summary>
public partial class InventoryView : GodotMviControlView<InventoryViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(InventoryViewModel viewModel, GodotMviDisposableBag bindings)
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
