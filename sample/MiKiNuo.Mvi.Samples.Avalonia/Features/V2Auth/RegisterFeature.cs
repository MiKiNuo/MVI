using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>通过生成输入和操作入口执行注册表单。</summary>
/// <param name="service">由宿主持有的认证服务。</param>
public sealed partial class RegisterFeature(IAuthService service) : Feature<RegisterState>(new())
{
    [Operation(Validate = nameof(CanSubmit))]
    private async Task<AuthResult> SubmitAsync(Operation<RegisterState> operation)
    {
        RegisterState state = operation.Snapshot;
        AuthResult result = await service.RegisterAsync(state.UserName, state.Email, state.Password, operation.CancellationToken).ConfigureAwait(false);
        operation.CancellationToken.ThrowIfCancellationRequested();
        await operation.UpdateAsync(ApplyResult, result);
        return result;
    }

    private static bool CanSubmit(RegisterState state) => state.ValidationError is null;

    private static RegisterState ApplyResult(RegisterState state, AuthResult result) => state with { Result = result };
}
