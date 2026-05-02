namespace MiKiNuo.Mvi.Domain.DI;

/// <summary>
/// 表示编译期 DI 应使用的构造函数。
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class DiConstructorAttribute : Attribute
{
}
