using System;
using System.Collections.Generic;
using global::Godot;

namespace MiKiNuo.Mvi.Platforms.Godot.Composition;

/// <summary>
/// 表示 Godot MVI View 注册表构建器。
/// </summary>
public sealed class GodotMviViewRegistryBuilder
{
    private readonly Dictionary<string, Func<Control>> _factories = new(StringComparer.Ordinal);

    /// <summary>
    /// 注册一个 Godot Control View。
    /// </summary>
    /// <typeparam name="TView">View 类型。</typeparam>
    /// <param name="key">View 注册键。</param>
    public void Register<TView>(string key)
        where TView : Control, new()
    {
        Register(key, static () => new TView());
    }

    /// <summary>
    /// 注册一个 Godot .tscn 场景 View。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    /// <param name="scenePath">Godot .tscn 场景路径，例如 res://Views/Dashboard/DashboardView.tscn。</param>
    public void RegisterScene(string key, string scenePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        Register(key, () => CreateFromScene(scenePath));
    }

    /// <summary>
    /// 注册一个 Godot Control View 工厂。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    /// <param name="factory">View 创建工厂。</param>
    public void Register(string key, Func<Control> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (!_factories.TryAdd(key, factory))
        {
            throw new InvalidOperationException($"Godot MVI View 注册键重复：{key}。");
        }
    }

    private static Control CreateFromScene(string scenePath)
    {
        PackedScene scene = GD.Load<PackedScene>(scenePath);
        Node instance = scene.Instantiate();

        if (instance is Control control)
        {
            return control;
        }

        instance.QueueFree();
        throw new InvalidOperationException($"Godot MVI 场景 {scenePath} 的根节点必须继承 Control。");
    }

    /// <summary>
    /// 构建不可变注册表。
    /// </summary>
    /// <returns>Godot View 注册表。</returns>
    public GodotMviViewRegistry Build()
    {
        return new GodotMviViewRegistry(new Dictionary<string, Func<Control>>(_factories, StringComparer.Ordinal));
    }
}
