using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

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
}
