using System;
using global::Godot;

namespace MiKiNuo.Mvi.Platforms.Godot.Composition;

/// <summary>
/// 提供 Godot MVI 组合式 UI 的 Slot 挂载能力。
/// </summary>
public static class GodotMviSceneHost
{
    /// <summary>
    /// 把 View 挂载到指定 Slot。
    /// </summary>
    /// <typeparam name="TView">View 类型。</typeparam>
    /// <param name="slot">承载 Slot。</param>
    /// <param name="view">要挂载的 View。</param>
    /// <param name="clearExisting">是否清理原有子节点。</param>
    /// <returns>已经挂载的 View。</returns>
    public static TView Mount<TView>(TView slot, TView view, bool clearExisting = true)
        where TView : Control
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(view);

        if (clearExisting)
        {
            Clear(slot);
        }

        view.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        view.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        slot.AddChild(view);
        return view;
    }

    /// <summary>
    /// 把 View 挂载到指定 Slot。
    /// </summary>
    /// <param name="slot">承载 Slot。</param>
    /// <param name="view">要挂载的 View。</param>
    /// <param name="clearExisting">是否清理原有子节点。</param>
    /// <returns>已经挂载的 View。</returns>
    public static Control Mount(Control slot, Control view, bool clearExisting = true)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(view);

        if (clearExisting)
        {
            Clear(slot);
        }

        view.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        view.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        slot.AddChild(view);
        return view;
    }

    /// <summary>
    /// 根据注册表键创建 View 并挂载到指定 Slot。
    /// </summary>
    /// <param name="slot">承载 Slot。</param>
    /// <param name="registry">View 注册表。</param>
    /// <param name="key">View 注册键。</param>
    /// <param name="clearExisting">是否清理原有子节点。</param>
    /// <returns>已经挂载的 View。</returns>
    public static Control MountRegistryView(
        Control slot,
        IGodotMviViewRegistry registry,
        string key,
        bool clearExisting = true)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return Mount(slot, registry.Create(key), clearExisting);
    }

    /// <summary>
    /// 清理 Slot 下的所有子节点。
    /// </summary>
    /// <param name="slot">承载 Slot。</param>
    public static void Clear(Control slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        foreach (Node child in slot.GetChildren())
        {
            child.QueueFree();
        }
    }
}
