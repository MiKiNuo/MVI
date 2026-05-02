namespace MiKiNuo.Mvi.Domain.MVI.Reducer;

/// <summary>
/// 表示可由源生成器收集的 MVI Reducer 方法。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MviReducerAttribute : Attribute
{
}
