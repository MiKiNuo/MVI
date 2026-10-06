using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
/// <summary>编排注册业务；异步服务是副作用，状态只能通过 Mutation 提交。</summary>
/// <param name="auth">认证服务。</param>
/// <param name="mediator">当前实例的范围内中介端点。</param>
[MviFeature]
public sealed partial class RegisterHandler(IAuthService auth, IMviMediator mediator) : MviIntentHandler<RegisterState, RegisterIntent>
{
    [MviHandle(typeof(RegisterIntent.Submit))]
    private async ValueTask SubmitAsync(RegisterIntent.Submit intent, IIntentContext<RegisterState> context, CancellationToken cancellationToken)
    {
        // 守卫与 Started 在 Store 内同锁提交，所有入口都受到保护。
        if (!context.TryReduce(static state => !state.IsBusy, new RegisterMutation.Started())) return;
        try
        {
            if (string.IsNullOrWhiteSpace(intent.UserName) || string.IsNullOrWhiteSpace(intent.Email) || string.IsNullOrWhiteSpace(intent.Password) || string.IsNullOrWhiteSpace(intent.ConfirmPassword) || intent.Password != intent.ConfirmPassword)
            { context.Reduce(new RegisterMutation.Failed("请填写完整信息，并确认两次密码一致。")); return; }
            AuthResult result = await auth.RegisterAsync(intent.UserName, intent.Email, intent.Password, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.IsSuccess) { context.Reduce(new RegisterMutation.Failed(result.ErrorMessage ?? "请求失败。")); return; }
            await mediator.SendAsync(new NavigateToPageRequest(ShellPage.Login), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.TryReduce(static _ => true, new RegisterMutation.Failed("操作未完成，请检查服务或诊断输出。"));
            throw;
        }
        finally
        {
            // 调用方取消仍可清理 Busy；Store 正在关闭时 TryReduce 返回 false。
            context.TryReduce(static state => state.IsBusy, new RegisterMutation.Finished());
        }
    }
    [MviHandle(typeof(RegisterIntent.GoLogin))]
    private ValueTask GoLoginAsync(RegisterIntent.GoLogin intent, IIntentContext<RegisterState> context, CancellationToken token)
        => NavigateAsync(ShellPage.Login, context, token);
    private async ValueTask NavigateAsync(ShellPage page, IIntentContext<RegisterState> context, CancellationToken token)
    {
        // 导航与提交互斥，因此当前最小表单不需要额外 FlowTicket 管线。
        if (!context.TryReduce(static state => !state.IsBusy, new RegisterMutation.Started())) return;
        try { await mediator.SendAsync(new NavigateToPageRequest(page), token); }
        finally { context.TryReduce(static state => state.IsBusy, new RegisterMutation.Finished()); }
    }
}
