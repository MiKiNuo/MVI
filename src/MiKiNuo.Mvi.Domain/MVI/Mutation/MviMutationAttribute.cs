namespace MiKiNuo.Mvi.Domain.MVI.Mutation;

/// <summary>
/// 标记变更记录并声明状态路径与操作。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviMutationAttribute : Attribute
{
    /// <summary>
    /// 获取或设置状态字段路径。
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置变更操作类型。
    /// </summary>
    public MutationOp Op { get; set; } = MutationOp.Set;

    /// <summary>
    /// 获取或设置变更值来源字段名。
    /// </summary>
    public string Source { get; set; } = "Value";
}
