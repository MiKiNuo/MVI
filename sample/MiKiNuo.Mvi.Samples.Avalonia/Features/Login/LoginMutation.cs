namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>登录状态的变化描述。</summary>
public abstract record LoginMutation : IMviMutation<LoginState>
{
    /// <summary>开始一次业务操作。</summary>
    public sealed record Started : LoginMutation;
    /// <summary>保存业务失败信息。</summary>
    /// <param name="Message">可公开的失败信息。</param>
    public sealed record Failed(string Message) : LoginMutation;
    /// <summary>结束一次业务操作，保留已有错误。</summary>
    /// <param name="Notice">可选完成提示。</param>
    public sealed record Finished(string? Notice = null) : LoginMutation;
}
