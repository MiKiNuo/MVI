using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RiskEventBoard;

/// <summary>
/// 表示风险事件 MVI视图。
/// </summary>
public sealed partial class RiskEventBoardView : MviAvaloniaView<RiskEventBoardViewModel>
{
    /// <summary>
    /// 初始化风险事件 MVI视图。
    /// </summary>
    public RiskEventBoardView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
