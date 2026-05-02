namespace MiKiNuo.Mvi.Abstractions.Generation;

/// <summary>
/// 标记当前类型由源生成器生成 Mediator 路由表。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviMediatorAttribute : Attribute
{
}
