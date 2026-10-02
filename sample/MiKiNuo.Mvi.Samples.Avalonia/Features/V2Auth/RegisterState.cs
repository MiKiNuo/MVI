using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>保存注册表单的输入与认证业务结果。</summary>
public sealed record RegisterState
{
    /// <summary>获取表单用户名输入，允许编辑中的中间值。</summary>
    [Input]
    public string UserName { get; init; } = string.Empty;

    /// <summary>获取表单邮箱输入，允许编辑中的中间值。</summary>
    [Input]
    public string Email { get; init; } = string.Empty;

    /// <summary>获取表单密码输入，允许编辑中的中间值。</summary>
    [Input]
    public string Password { get; init; } = string.Empty;

    /// <summary>获取表单确认密码输入，允许编辑中的中间值。</summary>
    [Input]
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>获取最近一次已提交的认证业务结果。</summary>
    public AuthResult? Result { get; init; }

    /// <summary>根据当前输入计算提交条件的详细错误。</summary>
    public string? ValidationError => AuthValidation.Register(UserName, Email, Password, ConfirmPassword)
        ?? (string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword) ? "请填写所有字段。" : null);
}
