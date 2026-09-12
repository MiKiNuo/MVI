namespace MiKiNuo.Mvi.Application.MVI.Mediator;

/// <summary>通知接纳尝试的状态。</summary>
public enum MviNotificationDeliveryStatus
{
    /// <summary>已接纳，不表示后续业务完成。</summary>
    Accepted,
    /// <summary>接纳回调抛出异常。</summary>
    Failed,
    /// <summary>取消、来源关闭或退订导致未尝试。</summary>
    NotAttempted,
    /// <summary>目标实例已经关闭。</summary>
    Closed,
}

/// <summary>单个订阅的投递结果。</summary>
/// <param name="TargetInstanceId">目标实例标识。</param>
/// <param name="Status">投递状态。</param>
/// <param name="Exception">失败时的异常。</param>
public sealed record MviNotificationDelivery(Guid TargetInstanceId, MviNotificationDeliveryStatus Status, Exception? Exception = null);

/// <summary>通知快照中所有订阅的接纳明细。</summary>
/// <param name="Deliveries">按注册顺序排列的只读明细。</param>
public sealed record MviNotificationReport(IReadOnlyList<MviNotificationDelivery> Deliveries);
