using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Domain.Common.Errors;
using MiKiNuo.Mvi.Domain.Common.Results;
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
    private readonly Func<TState, TIntent, OperationResult> _validator;
    private readonly IMviDiagnosticSink _diagnosticSink;
    private readonly string _componentName;

    /// <summary>
    /// 初始化 MVI 校验中间件。
    /// </summary>
    /// <param name="validator">校验委托；返回 <see cref="OperationResult.Success()"/> 表示通过，<see cref="OperationResult.Failure(DomainError)"/> 表示阻断。</param>
    /// <param name="diagnosticSink">诊断接收器。</param>
    /// <param name="componentName">组件名称。</param>
    public MviValidationMiddleware(
        Func<TState, TIntent, OperationResult> validator,
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

    /// <summary>
    /// 调用中间件。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="nextMiddleware">下一个中间件。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    public ValueTask<MviReduceResult<TState, TEffect>> InvokeAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        MviMiddlewareStep<TState, TIntent, TEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        OperationResult result = _validator(context.State, context.Intent);
        if (result.IsFailure)
        {
            string reason = result.FailureReason?.Message ?? result.FailureReason?.Code ?? "未知校验失败";
            _diagnosticSink.Record(new MviDiagnosticEntry(
                DateTimeOffset.Now,
                _componentName,
                "Validation",
                $"阻断 {typeof(TIntent).Name}：{reason}",
                0));
            return ValueTask.FromResult(MviReduceResult.State<TState, TEffect>(context.State));
        }

        _diagnosticSink.Record(new MviDiagnosticEntry(
            DateTimeOffset.Now,
            _componentName,
            "Validation",
            $"通过 {typeof(TIntent).Name}",
            0));
        return nextMiddleware(context, cancellationToken);
    }
}
