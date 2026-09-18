namespace MiKiNuo.Mvi.Domain.MVI.Mediator;

/// <summary>
/// 表示 EffectDispatcher 接纳方法与通知契约的声明式映射。
/// </summary>
/// <remarks>
/// 标注在 MviEffectDispatcherBase 子类的接纳方法上，声明本 Feature 订阅该通知，
/// 组合构建器据此在组合范围内建立显式订阅。
/// 签名约定为 (TNotification notification) =&gt; void，回调只能同步接纳本地意图，
/// 拒绝接纳时须抛出异常；方法可见性须为 internal 或 public。
/// </remarks>
/// <param name="notificationType">通知契约类型，须实现 IMviNotification。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MviNotificationAcceptorAttribute(Type notificationType) : Attribute
{
    /// <summary>
    /// 获取通知契约类型。
    /// </summary>
    public Type NotificationType { get; } = notificationType;
}
