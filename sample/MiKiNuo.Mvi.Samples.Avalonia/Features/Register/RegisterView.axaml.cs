using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页视图。
/// </summary>
public sealed partial class RegisterView : MviAvaloniaView<RegisterViewModel>
{
    /// <summary>
    /// 初始化注册页视图。
    /// </summary>
    public RegisterView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
