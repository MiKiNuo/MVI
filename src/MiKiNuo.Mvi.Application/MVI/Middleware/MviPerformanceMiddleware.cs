using System.Diagnostics;
using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.Middleware;

/// <summary>
/// 表示用于统计 Intent 规约耗时的 MVI 性能中间件。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviPerformanceMiddleware<TState, TIntent, TEffect> : IMviMiddleware<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IMviDiagnosticSink _diagnosticSink;
    private readonly string _componentName;

    /// <summary>
    /// 初始化 MVI 性能中间件。
    /// </summary>
    /// <param name="diagnosticSink">诊断接收器。</param>
    /// <param name="componentName">组件名称。</param>
    public MviPerformanceMiddleware(IMviDiagnosticSink diagnosticSink, string componentName)
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

        Stopwatch stopwatch = Stopwatch.StartNew();
        MviReduceResult<TState, TEffect> result = await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        _diagnosticSink.Record(new MviDiagnosticEntry(
            DateTimeOffset.Now,
            _componentName,
            "Middleware",
            $"{typeof(TIntent).Name} 规约耗时",
            stopwatch.ElapsedMilliseconds));

        return result;
    }
}
