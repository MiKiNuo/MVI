namespace MiKiNuo.Mvi.Application.MVI.Component;

/// <summary>
/// 表示 MVI 组件树节点。
/// </summary>
public interface IMviComponentNode
{
    /// <summary>
    /// 获取组件编号。
    /// </summary>
    public MviComponentId Id { get; }

    /// <summary>
    /// 获取父组件。
    /// </summary>
    public IMviComponentNode? Parent { get; }

    /// <summary>
    /// 获取子组件集合。
    /// </summary>
    public IReadOnlyList<IMviComponentNode> Children { get; }
}
