namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>登录页的业务状态，不包含输入凭据。</summary>
/// <param name="IsBusy">当前业务操作是否执行中。</param>
/// <param name="ErrorMessage">业务失败信息。</param>
/// <param name="Notice">完成提示。</param>
public sealed record LoginState(bool IsBusy = false, string? ErrorMessage = null, string? Notice = null) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static LoginState Initial { get; } = new();
}
