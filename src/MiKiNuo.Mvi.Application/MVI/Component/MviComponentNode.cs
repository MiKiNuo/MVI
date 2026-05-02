namespace MiKiNuo.Mvi.Application.MVI.Component;

/// <summary>
/// 表示默认 MVI 组件树节点。
/// </summary>
/// <param name="id">组件编号。</param>
/// <param name="parent">父组件。</param>
public sealed class MviComponentNode(MviComponentId id, IMviComponentNode? parent = null) : IMviComponentNode
{
    private readonly List<IMviComponentNode> _children = [];

    /// <inheritdoc />
    public MviComponentId Id { get; } = id;

    /// <inheritdoc />
    public IMviComponentNode? Parent { get; } = parent;

    /// <inheritdoc />
    public IReadOnlyList<IMviComponentNode> Children => _children;

    /// <summary>
    /// 添加子节点。
    /// </summary>
    /// <param name="child">子节点。</param>
    public void AddChild(IMviComponentNode child)
    {
        _children.Add(child);
    }
}
