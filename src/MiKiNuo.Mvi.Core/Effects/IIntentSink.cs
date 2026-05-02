using MiKiNuo.Mvi.Abstractions;

namespace MiKiNuo.Mvi.Core.Effects;

/// <summary>
/// 表示副作用处理器可用的意图接收器。
/// </summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
public interface IIntentSink<TIntent>
    where TIntent : IMviIntent
{
    /// <summary>
    /// 派发后续意图。
    /// </summary>
    /// <param name="intent">后续意图。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken);
}
