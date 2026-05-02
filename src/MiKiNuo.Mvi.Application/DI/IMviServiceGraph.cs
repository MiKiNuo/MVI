namespace MiKiNuo.Mvi.Application.DI;

/// <summary>
/// 表示编译期生成的服务依赖图。
/// </summary>
public interface IMviServiceGraph
{
    /// <summary>
    /// 获取服务描述集合。
    /// </summary>
    public IReadOnlyList<MviServiceDescriptor> ServiceDescriptors { get; }
}
