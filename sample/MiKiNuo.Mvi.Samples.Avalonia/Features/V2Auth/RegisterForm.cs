using Avalonia;
using Avalonia.Controls;
using MiKiNuo.Mvi.Platforms.Avalonia;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>通过原生控件和生成投影展示注册表单。</summary>
public sealed class RegisterForm : UserControl, IDisposable
{
    private readonly List<IDisposable> inputs = [];

    /// <summary>创建独立完整 Feature 的本地表单 View。</summary>
    /// <param name="service">由宿主持有的认证服务。</param>
    public RegisterForm(IAuthService service) : this(new RegisterFeature(service ?? throw new ArgumentNullException(nameof(service))))
    {
    }

    /// <summary>为已由宿主持有的注册实例创建本次本地 View。</summary>
    /// <param name="feature">保留业务状态与执行的现有实例。</param>
    public RegisterForm(RegisterFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        Feature = feature;
        Projection = AvaloniaProjection.Create<RegisterFeature.Projection>(Feature.CreateProjection);
        DataContext = Projection;
        Feedback = new(Projection.SubmitAsyncCommand, static state => state.ValidationError, static state => state.Result);
        StackPanel panel = new() { Margin = new Thickness(32), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "注册", FontSize = 24 });
        UserNameInput = new TextBox { PlaceholderText = "用户名" };
        inputs.Add(AvaloniaProjection.BindInput(Projection, UserNameInput, TextBox.TextProperty, static view => view.UserName));
        panel.Children.Add(new TextBlock { Text = "用户名" });
        panel.Children.Add(UserNameInput);
        EmailInput = new TextBox { PlaceholderText = "邮箱" };
        inputs.Add(AvaloniaProjection.BindInput(Projection, EmailInput, TextBox.TextProperty, static view => view.Email));
        panel.Children.Add(new TextBlock { Text = "邮箱" });
        panel.Children.Add(EmailInput);
        PasswordInput = new TextBox { PlaceholderText = "密码", PasswordChar = '●' };
        inputs.Add(AvaloniaProjection.BindInput(Projection, PasswordInput, TextBox.TextProperty, static view => view.Password));
        panel.Children.Add(new TextBlock { Text = "密码" });
        panel.Children.Add(PasswordInput);
        ConfirmPasswordInput = new TextBox { PlaceholderText = "确认密码", PasswordChar = '●' };
        inputs.Add(AvaloniaProjection.BindInput(Projection, ConfirmPasswordInput, TextBox.TextProperty, static view => view.ConfirmPassword));
        panel.Children.Add(new TextBlock { Text = "确认密码" });
        panel.Children.Add(ConfirmPasswordInput);
        panel.Children.Add(Feedback);
        Content = panel;
    }

    internal RegisterFeature Feature { get; }
    internal RegisterFeature.Projection Projection { get; }
    internal AuthFormFeedback<RegisterState> Feedback { get; }
    internal TextBox UserNameInput { get; }
    internal TextBox EmailInput { get; }
    internal TextBox PasswordInput { get; }
    internal TextBox ConfirmPasswordInput { get; }

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
