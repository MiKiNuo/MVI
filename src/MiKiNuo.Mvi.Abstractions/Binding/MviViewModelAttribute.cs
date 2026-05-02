namespace MiKiNuo.Mvi.Abstractions.Binding;

/// <summary>
/// 标记当前类型为 MVI ViewModel，由源生成器生成绑定成员。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviViewModelAttribute : Attribute
{
}
