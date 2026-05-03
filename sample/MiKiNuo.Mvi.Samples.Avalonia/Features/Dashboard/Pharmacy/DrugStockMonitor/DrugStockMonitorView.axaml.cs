using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.DrugStockMonitor;

/// <summary>
/// 表示库存监控 MVI视图。
/// </summary>
public sealed partial class DrugStockMonitorView : MviAvaloniaView<DrugStockMonitorViewModel>
{
    /// <summary>
    /// 初始化库存监控 MVI视图。
    /// </summary>
    public DrugStockMonitorView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
