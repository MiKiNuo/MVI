namespace MiKiNuo.Mvi.Domain.MVI.Mediator;

/// <summary>
/// 表示 EffectDispatcher 处理方法与请求契约的声明式映射。
/// </summary>
/// <remarks>
/// 标注在 MviEffectDispatcherBase 子类的处理方法上，声明本 Feature 是该请求契约的提供方，
/// 组合构建器据此在组合范围内注册路由处理器并把消费方绑定到本实例。
/// 签名约定为 (TRequest request, CancellationToken cancellationToken) =&gt; ValueTask&lt;TResponse&gt;，
/// 方法可见性须为 internal 或 public（生成的组合构建器与分发器位于同一程序集）。
/// </remarks>
/// <param name="requestType">请求契约类型，须实现 IMviRequest&lt;TResponse&gt;。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MviRouteHandlerAttribute(Type requestType) : Attribute
{
    /// <summary>
    /// 获取请求契约类型。
    /// </summary>
    public Type RequestType { get; } = requestType;
}
