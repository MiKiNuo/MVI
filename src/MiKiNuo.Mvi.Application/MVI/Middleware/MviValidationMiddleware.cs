using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.Middleware;

/// <summary>
/// 表示支持短路校验的 MVI 中间件。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviValidationMiddleware<TState, TIntent, TEffect> : IMviMiddleware<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly Func<TState, TIntent, string?> _validator;
    private readonly IMviDiagnosticSink _diagnosticSink;
    private readonly string _componentName;

    /// <summary>
    /// 初始化 MVI 校验中间件。
    /// </summary>
    /// <param name="validator">校验委托，返回空表示通过，返回非空表示阻断。</param>
    /// <param name="diagnosticSink">诊断接收器。</param>
    /// <param name="componentName">组件名称。</param>
    public MviValidationMiddleware(
        Func<TState, TIntent, string?> validator,
        IMviDiagnosticSink diagnosticSink,
        string componentName)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(diagnosticSink);
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);

        _validator = validator;
        _diagnosticSink = diagnosticSink;
        _componentName = componentName;
    }

    /// <inheritdoc />
    public ValueTask<MviReduceResult<TState, TEffect>> InvokeAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        MviMiddlewareStep<TState, TIntent, TEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        string? validationMessage = _validator(context.State, context.Intent);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            _diagnosticSink.Record(new MviDiagnosticEntry(
                DateTimeOffset.Now,
                _componentName,
                "Validation",
                $"阻断 {context.Intent.GetType().Name}：{validationMessage}",
                0));
            return ValueTask.FromResult(MviReduceResult.State<TState, TEffect>(context.State));
        }

        _diagnosticSink.Record(new MviDiagnosticEntry(
            DateTimeOffset.Now,
            _componentName,
            "Validation",
            $"通过 {context.Intent.GetType().Name}",
            0));
        return nextMiddleware(context, cancellationToken);
    }
}
