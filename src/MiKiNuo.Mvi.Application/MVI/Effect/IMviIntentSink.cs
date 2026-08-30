using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Application.MVI.Effect;

/// <summary>
/// 表示 Intent 回流入口，由 Store 实现并注入 EffectDispatcher。
/// </summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
public interface IMviIntentSink<in TIntent>
    where TIntent : IMviIntent
{
    /// <summary>
    /// 派发意图。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default);
}

/// <summary>
/// 表示可附加 Intent 回流入口的副作用分发器，由 MviStore 在构造时识别并接线。
/// </summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
internal interface IMviIntentSinkAttachable<TIntent>
    where TIntent : IMviIntent
{
    /// <summary>
    /// 附加 Intent 回流入口。
    /// </summary>
    /// <param name="sink">回流入口。</param>
    public void Attach(IMviIntentSink<TIntent> sink);
}
