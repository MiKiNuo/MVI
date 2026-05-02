namespace MiKiNuo.Mvi.Abstractions.Generation;

/// <summary>
/// 标记当前类型为 MVI 意图联合基类。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviIntentUnionAttribute : Attribute
{
}
