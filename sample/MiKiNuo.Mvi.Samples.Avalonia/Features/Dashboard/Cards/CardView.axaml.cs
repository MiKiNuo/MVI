using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片视图（同时承载 Simple Card 和 Form Card 两种布局，通过 CardViewModel.IsFormCard 切换）。
/// 必须继承 <see cref="MviAvaloniaView{TViewModel}"/> 才能被 DiContainerGenerator 扫描到 SampleGeneratedViewRegistry，
/// 否则 BusinessCompositePageView 渲染 4 张卡片时会抛 <c>MviViewNotFoundException</c>。
/// </summary>
public sealed partial class CardView : MviAvaloniaView<CardViewModel>
{
    /// <summary>
    /// 初始化仪表板卡片视图。
    /// </summary>
    public CardView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// BedOverview 卡片上 ComboBox 选中项变更回调。
    /// 通过 <see cref="CardViewModel.SetBedFilterAsync"/> 派发 <see cref="CardIntent.SetBedFilter"/> 意图，
    /// 由 <see cref="CardReducer"/> 统一处理；reducer 对非 BedOverview 卡片短路忽略。
    /// </summary>
    /// <param name="sender">触发事件的 ComboBox。</param>
    /// <param name="e">事件参数。</param>
    private void OnBedFilterSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        if (comboBox.SelectedItem is not BedFilterOption option)
        {
            return;
        }

        if (DataContext is not CardViewModel viewModel)
        {
            return;
        }

#pragma warning disable CA2012
        _ = viewModel.SetBedFilterAsync(option.Value);
#pragma warning restore CA2012
    }

    /// <summary>
    /// 床位类型 CheckBox 勾选状态变更回调。
    /// 通过 <see cref="CardViewModel.ToggleBedTypeAsync"/> 派发 <see cref="CardIntent.ToggleBedType"/> 意图；
    /// CheckBox.Tag 携带目标 <see cref="BedType"/> 枚举值；e.IsChecked 表示目标勾选状态。
    /// </summary>
    /// <param name="sender">触发事件的 CheckBox。</param>
    /// <param name="e">事件参数（包含新的 IsChecked 状态）。</param>
    private void OnBedTypeCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox)
        {
            return;
        }

        if (checkBox.Tag is not BedType bedType)
        {
            return;
        }

        if (DataContext is not CardViewModel viewModel)
        {
            return;
        }

        bool isChecked = checkBox.IsChecked ?? false;
#pragma warning disable CA2012
        _ = viewModel.ToggleBedTypeAsync(bedType, isChecked);
#pragma warning restore CA2012
    }

    /// <summary>
    /// 床位状态 CheckBox 勾选状态变更回调。
    /// 通过 <see cref="CardViewModel.ToggleBedStatusAsync"/> 派发 <see cref="CardIntent.ToggleBedStatus"/> 意图；
    /// CheckBox.Tag 携带目标 <see cref="BedStatus"/> 枚举值；e.IsChecked 表示目标勾选状态。
    /// </summary>
    /// <param name="sender">触发事件的 CheckBox。</param>
    /// <param name="e">事件参数（包含新的 IsChecked 状态）。</param>
    private void OnBedStatusCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox)
        {
            return;
        }

        if (checkBox.Tag is not BedStatus bedStatus)
        {
            return;
        }

        if (DataContext is not CardViewModel viewModel)
        {
            return;
        }

        bool isChecked = checkBox.IsChecked ?? false;
#pragma warning disable CA2012
        _ = viewModel.ToggleBedStatusAsync(bedStatus, isChecked);
#pragma warning restore CA2012
    }
}
