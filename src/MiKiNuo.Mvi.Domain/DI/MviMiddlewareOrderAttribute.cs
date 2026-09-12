namespace MiKiNuo.Mvi.Domain.DI;

/// <summary>指定 Feature 中间件的进入顺序，数值小者先执行。</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviMiddlewareOrderAttribute : Attribute
{
    /// <summary>建立显式顺序声明。</summary>
    /// <param name="order">同一 Feature 内唯一的顺序值。</param>
    public MviMiddlewareOrderAttribute(int order) => Order = order;

    /// <summary>获取中间件进入顺序。</summary>
    public int Order { get; }
}
