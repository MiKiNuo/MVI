using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页视图。
/// </summary>
public sealed partial class ResetPasswordView : MviAvaloniaView<ResetPasswordViewModel>
{
    /// <summary>
    /// 初始化重置密码页视图。
    /// </summary>
    public ResetPasswordView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
