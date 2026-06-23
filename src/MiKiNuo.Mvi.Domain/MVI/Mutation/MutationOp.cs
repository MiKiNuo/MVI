namespace MiKiNuo.Mvi.Domain.MVI.Mutation;

/// <summary>
/// 表示变更操作类型。
/// </summary>
public enum MutationOp
{
    /// <summary>
    /// 设置字段值。
    /// </summary>
    Set = 0,

    /// <summary>
    /// 累加数值字段。
    /// </summary>
    Add = 1,

    /// <summary>
    /// 追加字符串字段。
    /// </summary>
    Append = 2,
}
