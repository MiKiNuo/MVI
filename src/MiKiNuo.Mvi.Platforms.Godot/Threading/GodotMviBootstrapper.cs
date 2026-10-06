using global::Godot;

namespace MiKiNuo.Mvi.Platforms.Godot.Threading;

/// <summary>
/// 提供 Godot MVI 平台启动配置能力。
/// </summary>
public static class GodotMviBootstrapper
{
    /// <summary>
    /// 安装 Godot MVI UI 调度器到指定根节点，并返回该调度器供 DI 容器注册。
    /// </summary>
    /// <param name="root">Godot 根节点或当前场景根节点。</param>
    /// <param name="name">调度器节点名称。</param>
    /// <returns>Godot MVI UI 调度器。</returns>
    public static GodotMviUiDispatcher Install(Node root, string name = "MiKiNuoMviUiDispatcher")
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        GodotMviUiDispatcher? dispatcher = root.GetNodeOrNull<GodotMviUiDispatcher>(name);
        if (dispatcher is null)
        {
            dispatcher = new GodotMviUiDispatcher
            {
                Name = name,
            };

            root.AddChild(dispatcher);
        }

        return dispatcher;
    }
}
