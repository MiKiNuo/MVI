using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录页审计中间件：记录每个进入管线的意图（含 Effect 回流意图），验证追踪链完整。
/// </summary>
public sealed class LoginAuditMiddleware : IMviMiddleware<LoginState, LoginIntent, LoginEffect>
{
    private readonly List<string> _trail = [];

    /// <summary>
    /// 获取已审计的意图名称序列。
    /// </summary>
    public IReadOnlyList<string> Trail => _trail;

    /// <summary>
    /// 记录意图并继续管线。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="nextMiddleware">下一个中间件。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    public async ValueTask<MviReduceResult<LoginState, LoginEffect>> InvokeAsync(
        MviMiddlewareContext<LoginState, LoginIntent, LoginEffect> context,
        MviMiddlewareStep<LoginState, LoginIntent, LoginEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        _trail.Add(context.Intent.GetType().Name);
        return await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
    }
}
