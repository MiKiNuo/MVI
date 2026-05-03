namespace MiKiNuo.Mvi.Domain.MVI.Reducer;

/// <summary>
/// 表示 MVI 规约方法特性。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MviReduceAttribute : Attribute
{
}
