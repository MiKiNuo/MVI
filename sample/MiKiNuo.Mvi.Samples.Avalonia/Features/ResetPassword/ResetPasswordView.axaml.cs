using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
/// <summary>重置密码 View；平台绑定接口由生成器提供，不继承框架控件基类。</summary>
[MviView(typeof(ResetPasswordViewModel))]
public sealed partial class ResetPasswordView : UserControl
{
    /// <summary>加载界面。</summary>
    public ResetPasswordView() => AvaloniaXamlLoader.Load(this);
}
