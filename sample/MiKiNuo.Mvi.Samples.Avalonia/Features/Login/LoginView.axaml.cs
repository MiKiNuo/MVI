using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>登录 View；平台绑定接口由生成器提供，不继承框架控件基类。</summary>
[MviView(typeof(LoginViewModel))]
public sealed partial class LoginView : UserControl
{
    /// <summary>加载界面。</summary>
    public LoginView() => AvaloniaXamlLoader.Load(this);
}
