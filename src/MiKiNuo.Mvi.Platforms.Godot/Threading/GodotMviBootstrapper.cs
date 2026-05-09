using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Threading;

namespace MiKiNuo.Mvi.Platforms.Godot.Threading;

/// <summary>
/// 提供 Godot MVI 平台启动配置能力。
/// </summary>
public static class GodotMviBootstrapper
{
    /// <summary>
    /// 安装 Godot MVI UI 调度器并接入通用 ViewModel UI 通知调度器。
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

        Configure(dispatcher);
        return dispatcher;
    }

    /// <summary>
    /// 把指定 Godot UI 调度器配置到 MVI Presentation 层。
    /// </summary>
    /// <param name="dispatcher">Godot UI 调度器。</param>
    public static void Configure(GodotMviUiDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        MviUiNotificationDispatcher.Configure(dispatcher.Post);
    }
}
