using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
/// <summary>编排重置密码业务；异步服务是副作用，状态只能通过 Mutation 提交。</summary>
/// <param name="auth">认证服务。</param>
/// <param name="mediator">当前实例的范围内中介端点。</param>
[MviFeature]
public sealed partial class ResetPasswordHandler(IAuthService auth, IMviMediator mediator) : MviIntentHandler<ResetPasswordState, ResetPasswordIntent>
{
    [MviHandle(typeof(ResetPasswordIntent.Submit))]
    private async ValueTask SubmitAsync(ResetPasswordIntent.Submit intent, IIntentContext<ResetPasswordState> context, CancellationToken cancellationToken)
    {
        // 守卫与 Started 在 Store 内同锁提交，所有入口都受到保护。
        if (!context.TryReduce(static state => !state.IsBusy, new ResetPasswordMutation.Started())) return;
        try
        {
            if (string.IsNullOrWhiteSpace(intent.UserName) || string.IsNullOrWhiteSpace(intent.NewPassword) || string.IsNullOrWhiteSpace(intent.ConfirmPassword) || intent.NewPassword != intent.ConfirmPassword)
            { context.Reduce(new ResetPasswordMutation.Failed("请填写完整信息，并确认两次密码一致。")); return; }
            AuthResult result = await auth.ResetPasswordAsync(intent.UserName, intent.NewPassword, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.IsSuccess) { context.Reduce(new ResetPasswordMutation.Failed(result.ErrorMessage ?? "请求失败。")); return; }
            await mediator.SendAsync(new NavigateToPageRequest(ShellPage.Login), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.TryReduce(static _ => true, new ResetPasswordMutation.Failed("操作未完成，请检查服务或诊断输出。"));
            throw;
        }
        finally
        {
            // 调用方取消仍可清理 Busy；Store 正在关闭时 TryReduce 返回 false。
            context.TryReduce(static state => state.IsBusy, new ResetPasswordMutation.Finished());
        }
    }
    [MviHandle(typeof(ResetPasswordIntent.GoLogin))]
    private ValueTask GoLoginAsync(ResetPasswordIntent.GoLogin intent, IIntentContext<ResetPasswordState> context, CancellationToken token)
        => NavigateAsync(ShellPage.Login, context, token);
    private async ValueTask NavigateAsync(ShellPage page, IIntentContext<ResetPasswordState> context, CancellationToken token)
    {
        // 导航与提交互斥，因此当前最小表单不需要额外 FlowTicket 管线。
        if (!context.TryReduce(static state => !state.IsBusy, new ResetPasswordMutation.Started())) return;
        try { await mediator.SendAsync(new NavigateToPageRequest(page), token); }
        finally { context.TryReduce(static state => state.IsBusy, new ResetPasswordMutation.Finished()); }
    }
}
