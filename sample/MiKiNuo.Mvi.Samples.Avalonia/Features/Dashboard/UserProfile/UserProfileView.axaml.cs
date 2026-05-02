using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件视图。
/// </summary>
public sealed partial class UserProfileView : MviAvaloniaView<UserProfileViewModel>
{
    /// <summary>
    /// 初始化用户信息组件视图。
    /// </summary>
    public UserProfileView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
