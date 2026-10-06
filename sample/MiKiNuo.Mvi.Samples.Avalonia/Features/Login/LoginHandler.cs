using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>编排登录业务；异步服务是副作用，状态只能通过 Mutation 提交。</summary>
/// <param name="auth">认证服务。</param>
/// <param name="mediator">当前实例的范围内中介端点。</param>
[MviFeature]
public sealed partial class LoginHandler(IAuthService auth, IMviMediator mediator) : MviIntentHandler<LoginState, LoginIntent>
{
    [MviHandle(typeof(LoginIntent.Submit))]
    private async ValueTask SubmitAsync(LoginIntent.Submit intent, IIntentContext<LoginState> context, CancellationToken cancellationToken)
    {
        // 守卫与 Started 在 Store 内同锁提交，所有入口都受到保护。
        if (!context.TryReduce(static state => !state.IsBusy, new LoginMutation.Started())) return;
        try
        {
            if (string.IsNullOrWhiteSpace(intent.UserName) || string.IsNullOrWhiteSpace(intent.Password))
            { context.Reduce(new LoginMutation.Failed("请输入用户名和密码。")); return; }
            AuthResult result = await auth.LoginAsync(intent.UserName, intent.Password, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.IsSuccess) { context.Reduce(new LoginMutation.Failed(result.ErrorMessage ?? "请求失败。")); return; }
            await mediator.SendAsync(new NavigateToPageRequest(ShellPage.Home, result.DisplayName), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.TryReduce(static _ => true, new LoginMutation.Failed("操作未完成，请检查服务或诊断输出。"));
            throw;
        }
        finally
        {
            // 调用方取消仍可清理 Busy；Store 正在关闭时 TryReduce 返回 false。
            context.TryReduce(static state => state.IsBusy, new LoginMutation.Finished());
        }
    }
    [MviHandle(typeof(LoginIntent.GoRegister))]
    private ValueTask GoRegisterAsync(LoginIntent.GoRegister intent, IIntentContext<LoginState> context, CancellationToken token)
        => NavigateAsync(ShellPage.Register, context, token);
    [MviHandle(typeof(LoginIntent.GoResetPassword))]
    private ValueTask GoResetPasswordAsync(LoginIntent.GoResetPassword intent, IIntentContext<LoginState> context, CancellationToken token)
        => NavigateAsync(ShellPage.ResetPassword, context, token);
    private async ValueTask NavigateAsync(ShellPage page, IIntentContext<LoginState> context, CancellationToken token)
    {
        // 导航与提交互斥，因此当前最小表单不需要额外 FlowTicket 管线。
        if (!context.TryReduce(static state => !state.IsBusy, new LoginMutation.Started())) return;
        try { await mediator.SendAsync(new NavigateToPageRequest(page), token); }
        finally { context.TryReduce(static state => state.IsBusy, new LoginMutation.Finished()); }
    }
}
