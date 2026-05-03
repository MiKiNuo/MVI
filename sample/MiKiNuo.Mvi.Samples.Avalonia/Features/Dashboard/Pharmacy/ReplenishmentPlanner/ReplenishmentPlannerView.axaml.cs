using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.ReplenishmentPlanner;

/// <summary>
/// 表示补货计划 MVI视图。
/// </summary>
public sealed partial class ReplenishmentPlannerView : MviAvaloniaView<ReplenishmentPlannerViewModel>
{
    /// <summary>
    /// 初始化补货计划 MVI视图。
    /// </summary>
    public ReplenishmentPlannerView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
