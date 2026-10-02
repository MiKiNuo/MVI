using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>保存登录表单的输入与认证业务结果。</summary>
public sealed record LoginState
{
    /// <summary>获取表单用户名输入，允许编辑中的中间值。</summary>
    [Input]
    public string UserName { get; init; } = string.Empty;

    /// <summary>获取表单密码输入，允许编辑中的中间值。</summary>
    [Input]
    public string Password { get; init; } = string.Empty;

    /// <summary>获取最近一次已提交的认证业务结果。</summary>
    public AuthResult? Result { get; init; }

    /// <summary>根据当前输入计算提交条件的详细错误。</summary>
    public string? ValidationError => string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password)
        ? "请输入用户名和密码。" : null;
}
