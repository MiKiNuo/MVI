using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
namespace MiKiNuo.Mvi.Runtime.MVI.Middleware;
/// <summary>冻结中间件次序，并对每次派发提供独立的单次 nextStep 防护。</summary>
/// <typeparam name="TState">组件状态。</typeparam>
/// <typeparam name="TIntent">组件意图。</typeparam>
internal sealed class MviMiddlewarePipeline<TState, TIntent>
    where TState : IMviState where TIntent : IMviIntent
{
    private readonly IMviMiddleware<TState, TIntent>[] _middlewares;
    private readonly IIntentHandler<TIntent, TState> _handler;
    internal MviMiddlewarePipeline(IReadOnlyList<IMviMiddleware<TState, TIntent>>? middlewares, IIntentHandler<TIntent, TState> handler)
    {
        _middlewares = middlewares?.ToArray() ?? [];
        _handler = handler;
        if (_middlewares.Any(static item => item is null)) throw new ArgumentException("中间件不能包含 null。", nameof(middlewares));
    }
    internal ValueTask InvokeAsync(TIntent intent, IIntentContext<TState> context, CancellationToken token)
        => InvokeAtAsync(0, intent, context, token);
    private ValueTask InvokeAtAsync(int index, TIntent intent, IIntentContext<TState> context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (index == _middlewares.Length) return _handler.HandleAsync(intent, context, token);
        int invoked = 0;
        return _middlewares[index].InvokeAsync(intent, context, () =>
        {
            if (Interlocked.Exchange(ref invoked, 1) != 0) throw new InvalidOperationException("同一次中间件不能重复调用 nextStep。");
            return InvokeAtAsync(index + 1, intent, context, token);
        }, token);
    }
}
