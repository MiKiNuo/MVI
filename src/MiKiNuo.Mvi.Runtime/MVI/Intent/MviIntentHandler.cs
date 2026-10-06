using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.State;
namespace MiKiNuo.Mvi.Runtime.MVI.Intent;
/// <summary>生成式业务 Handler 的可选基类，同时提供本组件的通信入口。</summary>
/// <typeparam name="TState">组件状态。</typeparam>
/// <typeparam name="TIntent">组件意图。</typeparam>
public abstract class MviIntentHandler<TState, TIntent>
    : IIntentHandler<TIntent, TState>, IMviIntentSinkAttachable<TIntent>
    where TState : IMviState where TIntent : IMviIntent
{
    private IMviIntentSink<TIntent>? _sink;
    /// <summary>由生成器实现类型化方法分派。</summary>
    /// <param name="intent">用户意图。</param>
    /// <param name="context">状态变化上下文。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>业务处理任务。</returns>
    public abstract ValueTask HandleAsync(TIntent intent, IIntentContext<TState> context, CancellationToken cancellationToken);
    /// <summary>只允许接到一个 Store，避免同一个 Handler 被不同组件借用。</summary>
    /// <param name="sink">所属 Store。</param>
    public void Attach(IMviIntentSink<TIntent> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        if (Interlocked.CompareExchange(ref _sink, sink, null) is not null)
            throw new InvalidOperationException("Handler 已绑定到 Store，不能复用。");
    }
    /// <summary>供公开路由把外部请求转换为本组件意图；不是跨组件 Store 访问。</summary>
    /// <param name="intent">本地意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>本地业务处理任务。</returns>
    protected ValueTask DispatchIntentAsync(TIntent intent, CancellationToken cancellationToken = default)
        => (_sink ?? throw new InvalidOperationException("Handler 尚未绑定 Store。")).DispatchAsync(intent, cancellationToken);
    /// <summary>供同步通知入口进行有界接纳。</summary>
    /// <param name="intent">本地意图。</param>
    /// <returns>是否接纳。</returns>
    protected bool TryAcceptIntent(TIntent intent) => _sink?.TryPost(intent) == true;
}
