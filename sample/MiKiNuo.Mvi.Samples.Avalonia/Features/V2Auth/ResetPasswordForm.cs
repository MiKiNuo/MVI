using Avalonia;
using Avalonia.Controls;
using MiKiNuo.Mvi.Platforms.Avalonia;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>通过原生控件和生成投影展示重置密码表单。</summary>
public sealed class ResetPasswordForm : UserControl, IDisposable
{
    private readonly List<IDisposable> inputs = [];

    /// <summary>创建独立完整 Feature 的本地表单 View。</summary>
    /// <param name="service">由宿主持有的认证服务。</param>
    public ResetPasswordForm(IAuthService service) : this(new ResetPasswordFeature(service ?? throw new ArgumentNullException(nameof(service))))
    {
    }

    /// <summary>为已由宿主持有的重置密码实例创建本次本地 View。</summary>
    /// <param name="feature">保留业务状态与执行的现有实例。</param>
    public ResetPasswordForm(ResetPasswordFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        Feature = feature;
        Projection = AvaloniaProjection.Create<ResetPasswordFeature.Projection>(Feature.CreateProjection);
        DataContext = Projection;
        Feedback = new(Projection.SubmitAsyncCommand, static state => state.ValidationError, static state => state.Result);
        StackPanel panel = new() { Margin = new Thickness(32), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "重置密码", FontSize = 24 });
        UserNameInput = new TextBox { PlaceholderText = "用户名" };
        inputs.Add(AvaloniaProjection.BindInput(Projection, UserNameInput, TextBox.TextProperty, static view => view.UserName));
        panel.Children.Add(new TextBlock { Text = "用户名" });
        panel.Children.Add(UserNameInput);
        NewPasswordInput = new TextBox { PlaceholderText = "新密码", PasswordChar = '●' };
        inputs.Add(AvaloniaProjection.BindInput(Projection, NewPasswordInput, TextBox.TextProperty, static view => view.NewPassword));
        panel.Children.Add(new TextBlock { Text = "新密码" });
        panel.Children.Add(NewPasswordInput);
        ConfirmPasswordInput = new TextBox { PlaceholderText = "确认密码", PasswordChar = '●' };
        inputs.Add(AvaloniaProjection.BindInput(Projection, ConfirmPasswordInput, TextBox.TextProperty, static view => view.ConfirmPassword));
        panel.Children.Add(new TextBlock { Text = "确认密码" });
        panel.Children.Add(ConfirmPasswordInput);
        panel.Children.Add(Feedback);
        Content = panel;
    }

    internal ResetPasswordFeature Feature { get; }
    internal ResetPasswordFeature.Projection Projection { get; }
    internal AuthFormFeedback<ResetPasswordState> Feedback { get; }
    internal TextBox UserNameInput { get; }
    internal TextBox NewPasswordInput { get; }
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
