using Avalonia;
using Avalonia.Controls;
using MiKiNuo.Mvi.Platforms.Avalonia;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>通过原生控件和生成投影展示登录表单。</summary>
public sealed class LoginForm : UserControl, IDisposable
{
    private readonly List<IDisposable> inputs = [];

    /// <summary>创建独立完整 Feature 的本地表单 View。</summary>
    /// <param name="service">由宿主持有的认证服务。</param>
    public LoginForm(IAuthService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        Feature = new LoginFeature(service);
        Projection = AvaloniaProjection.Create<LoginFeature.Projection>(Feature.CreateProjection);
        DataContext = Projection;
        Feedback = new(Projection.SubmitAsyncCommand, static state => state.ValidationError, static state => state.Result);
        StackPanel panel = new() { Margin = new Thickness(32), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "登录", FontSize = 24 });
        UserNameInput = new TextBox { PlaceholderText = "用户名" };
        inputs.Add(AvaloniaProjection.BindInput(Projection, UserNameInput, TextBox.TextProperty, static view => view.UserName));
        panel.Children.Add(new TextBlock { Text = "用户名" });
        panel.Children.Add(UserNameInput);
        PasswordInput = new TextBox { PlaceholderText = "密码", PasswordChar = '●' };
        inputs.Add(AvaloniaProjection.BindInput(Projection, PasswordInput, TextBox.TextProperty, static view => view.Password));
        panel.Children.Add(new TextBlock { Text = "密码" });
        panel.Children.Add(PasswordInput);
        panel.Children.Add(Feedback);
        Content = panel;
    }

    internal LoginFeature Feature { get; }
    internal LoginFeature.Projection Projection { get; }
    internal AuthFormFeedback<LoginState> Feedback { get; }
    internal TextBox UserNameInput { get; }
    internal TextBox PasswordInput { get; }

    /// <summary>断开该 View 的输入、命令与快照展示连接。</summary>
    public void Dispose()
    {
        foreach (IDisposable input in inputs) input.Dispose();
        Feedback.SubmitButton.Command = null;
        Projection.Dispose();
        DataContext = null;
        GC.SuppressFinalize(this);
    }
}
