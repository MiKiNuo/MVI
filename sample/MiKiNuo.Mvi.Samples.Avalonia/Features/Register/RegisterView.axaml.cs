using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
/// <summary>注册 View；平台绑定接口由生成器提供，不继承框架控件基类。</summary>
[MviView(typeof(RegisterViewModel))]
public sealed partial class RegisterView : UserControl
{
    /// <summary>加载界面。</summary>
    public RegisterView() => AvaloniaXamlLoader.Load(this);
}
