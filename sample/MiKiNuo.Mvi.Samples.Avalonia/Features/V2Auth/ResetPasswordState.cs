using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>保存重置密码表单的输入与认证业务结果。</summary>
public sealed record ResetPasswordState
{
    /// <summary>获取表单用户名输入，允许编辑中的中间值。</summary>
    [Input]
    public string UserName { get; init; } = string.Empty;

    /// <summary>获取表单新密码输入，允许编辑中的中间值。</summary>
    [Input]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>获取表单确认密码输入，允许编辑中的中间值。</summary>
    [Input]
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>获取最近一次已提交的认证业务结果。</summary>
    public AuthResult? Result { get; init; }

    /// <summary>根据当前输入计算提交条件的详细错误。</summary>
    public string? ValidationError => AuthValidation.ResetPassword(UserName, NewPassword, ConfirmPassword)
        ?? (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword) ? "请填写所有字段。" : null);
}
