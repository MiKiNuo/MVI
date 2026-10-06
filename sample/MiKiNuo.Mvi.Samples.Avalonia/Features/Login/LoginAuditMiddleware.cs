using System.Diagnostics;
using MiKiNuo.Mvi.Runtime.MVI.Middleware;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>实际接入登录管线的审计中间件；仅记录类型、耗时，不记录输入值。</summary>
[MviMiddlewareOrder(0)]
public sealed class LoginAuditMiddleware : IMviMiddleware<LoginState, LoginIntent>
{
    /// <inheritdoc/>
    public async ValueTask InvokeAsync(LoginIntent intent, IIntentContext<LoginState> context, MviNext nextStep, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        long start = Stopwatch.GetTimestamp();
        try { await nextStep(); }
        finally { Trace.WriteLine($"Login/{intent.GetType().Name}: {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F1} ms"); }
    }
}
