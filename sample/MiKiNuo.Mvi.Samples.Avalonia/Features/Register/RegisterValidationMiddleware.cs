using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页表单校验中间件：拦截 Submit 意图做字段规则校验，
/// 失败时写回带错误消息的状态并阻断后续规约与副作用，Reducer 因此保持纯粹。
/// </summary>
/// <remarks>
/// 职责划分：Reducer 的 CanSubmit 只负责"字段是否填齐"以驱动按钮可用态；
/// 具体业务规则（邮箱格式、密码长度、两次密码一致）由本中间件承载。
/// </remarks>
public sealed class RegisterValidationMiddleware
    : IMviMiddleware<RegisterState, RegisterIntent, RegisterEffect>
{
    /// <summary>
    /// 校验 Submit 意图，失败时阻断并写回错误消息。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="nextMiddleware">下一个中间件。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    public async ValueTask<MviReduceResult<RegisterState, RegisterEffect>> InvokeAsync(
        MviMiddlewareContext<RegisterState, RegisterIntent, RegisterEffect> context,
        MviMiddlewareStep<RegisterState, RegisterIntent, RegisterEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        if (context.Intent is not RegisterIntent.Submit)
        {
            return await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        }

        string? error = Validate(context.State);
        if (error is not null)
        {
            return MviReduceResult.State<RegisterState, RegisterEffect>(
                context.State with { ErrorMessage = error });
        }

        return await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
    }

    private static string? Validate(RegisterState state)
        => AuthValidation.Register(state.UserName, state.Email, state.Password, state.ConfirmPassword);
}
