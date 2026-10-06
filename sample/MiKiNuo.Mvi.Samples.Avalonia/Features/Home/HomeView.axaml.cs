using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>大厅 View，使用生成的非继承式接入。</summary>
[MviView(typeof(HomeViewModel))]
public sealed partial class HomeView : UserControl
{
    /// <summary>加载界面。</summary>
    public HomeView() => AvaloniaXamlLoader.Load(this);
}
