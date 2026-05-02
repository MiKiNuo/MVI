using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.Middleware;

/// <summary>
/// 表示用于记录 Intent、Reducer 和 Effect 流转的 MVI 日志中间件。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviLoggingMiddleware<TState, TIntent, TEffect> : IMviMiddleware<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IMviDiagnosticSink _diagnosticSink;
    private readonly string _componentName;

    /// <summary>
    /// 初始化 MVI 日志中间件。
    /// </summary>
    /// <param name="diagnosticSink">诊断接收器。</param>
    /// <param name="componentName">组件名称。</param>
    public MviLoggingMiddleware(IMviDiagnosticSink diagnosticSink, string componentName)
    {
        ArgumentNullException.ThrowIfNull(diagnosticSink);
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);

        _diagnosticSink = diagnosticSink;
        _componentName = componentName;
    }

    /// <inheritdoc />
    public async ValueTask<MviReduceResult<TState, TEffect>> InvokeAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        MviMiddlewareStep<TState, TIntent, TEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        Record("Intent", $"收到 {context.Intent.GetType().Name}", 0);
        MviReduceResult<TState, TEffect> result = await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        Record("Reducer", $"完成 {context.Intent.GetType().Name}，产生 {result.Effects.Count} 个 Effect", 0);

        foreach (TEffect effect in result.Effects)
        {
            Record("Effect", $"产生 {effect.GetType().Name}", 0);
        }

        return result;
    }

    private void Record(string stage, string message, long elapsedMilliseconds)
    {
        _diagnosticSink.Record(new MviDiagnosticEntry(
            DateTimeOffset.Now,
            _componentName,
            stage,
            message,
            elapsedMilliseconds));
    }
}
