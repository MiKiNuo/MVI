namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
/// <summary>重置密码状态的变化描述。</summary>
public abstract record ResetPasswordMutation : IMviMutation<ResetPasswordState>
{
    /// <summary>开始一次业务操作。</summary>
    public sealed record Started : ResetPasswordMutation;
    /// <summary>保存业务失败信息。</summary>
    /// <param name="Message">可公开的失败信息。</param>
    public sealed record Failed(string Message) : ResetPasswordMutation;
    /// <summary>结束一次业务操作，保留已有错误。</summary>
    /// <param name="Notice">可选完成提示。</param>
    public sealed record Finished(string? Notice = null) : ResetPasswordMutation;
}
