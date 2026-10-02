using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>通过生成输入和操作入口执行重置密码表单。</summary>
/// <param name="service">由宿主持有的认证服务。</param>
public sealed partial class ResetPasswordFeature(IAuthService service) : Feature<ResetPasswordState>(new())
{
    [Operation(Validate = nameof(CanSubmit))]
    private async Task<AuthResult> SubmitAsync(Operation<ResetPasswordState> operation)
    {
        ResetPasswordState state = operation.Snapshot;
        AuthResult result = await service.ResetPasswordAsync(state.UserName, state.NewPassword, operation.CancellationToken).ConfigureAwait(false);
        operation.CancellationToken.ThrowIfCancellationRequested();
        await operation.UpdateAsync(ApplyResult, result);
        return result;
    }

    private static bool CanSubmit(ResetPasswordState state) => state.ValidationError is null;

    private static ResetPasswordState ApplyResult(ResetPasswordState state, AuthResult result) => state with { Result = result };
}
