namespace MiKiNuo.Mvi.Domain.MVI.Reducer;

/// <summary>
/// 标记变更规约处理方法。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MviReduceMutationAttribute : Attribute
{
}
