using Avalonia.Controls;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>用本地标签展示三个彼此独立的完整 v2 认证 Feature。</summary>
public sealed class AuthFormsWindow : Window, IDisposable
{
    private readonly HttpAuthService? ownedService;

    /// <summary>创建认证表单窗口；默认使用现有联网认证服务。</summary>
    /// <param name="service">可选的外部认证服务，由调用方拥有。</param>
    public AuthFormsWindow(IAuthService? service = null)
    {
        if (service is null) service = ownedService = new HttpAuthService();
        Login = new(service);
        Register = new(service);
        ResetPassword = new(service);
        Title = "MVI v2 — 认证表单";
        Width = 560;
        Height = 720;
        Tabs = new TabControl
        {
            Items =
            {
                new TabItem { Header = "登录", Content = Login },
                new TabItem { Header = "注册", Content = Register },
                new TabItem { Header = "重置密码", Content = ResetPassword }
            }
        };
        Content = Tabs;
        Closed += (_, _) => Dispose();
    }

    internal LoginForm Login { get; }
    internal RegisterForm Register { get; }
    internal ResetPasswordForm ResetPassword { get; }
    internal TabControl Tabs { get; }

    /// <summary>释放表单展示连接与窗口创建的认证服务。</summary>
    public void Dispose()
    {
        Login.Dispose();
        Register.Dispose();
        ResetPassword.Dispose();
        ownedService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
