using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页表单校验中间件：拦截 Submit 意图做字段规则校验，
/// 失败时写回带错误消息的状态并阻断后续规约与副作用。
/// </summary>
public sealed class ResetPasswordValidationMiddleware
    : IMviMiddleware<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect>
{
    /// <summary>
    /// 校验 Submit 意图，失败时阻断并写回错误消息。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="nextMiddleware">下一个中间件。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    public async ValueTask<MviReduceResult<ResetPasswordState, ResetPasswordEffect>> InvokeAsync(
        MviMiddlewareContext<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect> context,
        MviMiddlewareStep<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        if (context.Intent is not ResetPasswordIntent.Submit)
        {
            return await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        }

        string? error = Validate(context.State);
        if (error is not null)
        {
            return MviReduceResult.State<ResetPasswordState, ResetPasswordEffect>(
                context.State with { ErrorMessage = error });
        }

        return await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
    }

    private static string? Validate(ResetPasswordState state)
    {
        if (state.UserName.Trim().Length < 3)
        {
            return "用户名至少需要 3 个字符。";
        }

        if (state.NewPassword.Length < 6)
        {
            return "新密码长度至少为 6 位。";
        }

        if (state.NewPassword != state.ConfirmPassword)
        {
            return "两次输入的密码不一致。";
        }

        return null;
    }
}
