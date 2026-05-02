using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录视图。
/// </summary>
public sealed partial class LoginView : MviAvaloniaView<LoginViewModel>
{
    /// <summary>
    /// 初始化登录视图。
    /// </summary>
    public LoginView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
