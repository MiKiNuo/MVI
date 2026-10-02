using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>通过生成输入和操作入口执行登录表单。</summary>
/// <param name="service">由宿主持有的认证服务。</param>
public sealed partial class LoginFeature(IAuthService service) : Feature<LoginState>(new())
{
    [Operation(Validate = nameof(CanSubmit))]
    private async Task<AuthResult> SubmitAsync(Operation<LoginState> operation)
    {
        LoginState state = operation.Snapshot;
        AuthResult result = await service.LoginAsync(state.UserName, state.Password, operation.CancellationToken).ConfigureAwait(false);
        operation.CancellationToken.ThrowIfCancellationRequested();
        await operation.UpdateAsync(ApplyResult, result);
        return result;
    }

    private static bool CanSubmit(LoginState state) => state.ValidationError is null;

    private static LoginState ApplyResult(LoginState state, AuthResult result) => state with { Result = result };
}
