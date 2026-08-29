using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Application.MVI.EventBinding;

/// <summary>
/// 表示 Intent 派发器接口，供 View 层把事件映射为 Intent 后派发到 Store。
/// </summary>
/// <remarks>
/// <see cref="MviComponent"/> 显式实现本接口；
/// View 通过 <see cref="MviComponent.GetIntentDispatcher"/> 获取实例，
/// 配合 <see cref="EventBinding{TEvent}"/> 把控件事件转换为 Intent 派发。
/// </remarks>
public interface IMviIntentDispatcher
{
    /// <summary>
    /// 当 Intent 派发失败时触发。
    /// </summary>
    public event EventHandler<IntentDispatchExceptionEventArgs>? DispatchFailed;

    /// <summary>
    /// 派发 Intent 到 Store。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public ValueTask DispatchAsync(IMviIntent intent, CancellationToken cancellationToken = default);
}

/// <summary>
/// 表示 Intent 派发失败事件参数。
/// </summary>
public sealed class IntentDispatchExceptionEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 Intent 派发失败事件参数。
    /// </summary>
    /// <param name="exception">派发异常。</param>
    /// <param name="intent">触发异常的 Intent。</param>
    public IntentDispatchExceptionEventArgs(Exception exception, IMviIntent intent)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(intent);
        Exception = exception;
        Intent = intent;
    }

    /// <summary>
    /// 获取派发异常。
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 获取触发异常的 Intent。
    /// </summary>
    public IMviIntent Intent { get; }
}
