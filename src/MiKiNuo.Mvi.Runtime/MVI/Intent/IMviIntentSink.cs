using MiKiNuo.Mvi.Abstractions.MVI.Intent;
namespace MiKiNuo.Mvi.Runtime.MVI.Intent;
/// <summary>仅供组件自身的路由和通知入口派发本地意图。</summary>
/// <typeparam name="TIntent">本组件的意图。</typeparam>
public interface IMviIntentSink<in TIntent> where TIntent : IMviIntent
{
    /// <summary>派发并等待完整业务操作。</summary>
    /// <param name="intent">本地意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>完整处理任务。</returns>
    public ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default);
    /// <summary>尝试将通知对应的本地意图放入有界队列。</summary>
    /// <param name="intent">本地意图。</param>
    /// <returns>仅表示队列接纳，不表示处理成功。</returns>
    public bool TryPost(TIntent intent);
}
/// <summary>将组件 Handler 接到其唯一 Store，不能跨 Store 复用同一个 Handler 实例。</summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
public interface IMviIntentSinkAttachable<TIntent> where TIntent : IMviIntent
{
    /// <summary>接入唯一的本地派发器。</summary>
    /// <param name="sink">本地 Store。</param>
    public void Attach(IMviIntentSink<TIntent> sink);
}
