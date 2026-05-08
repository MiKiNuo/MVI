using System;

namespace MiKiNuo.Mvi.Platforms.Godot.Composition;

/// <summary>
/// 标记一个 Godot MVI View 可以被编译期 ViewRegistry 注册。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviGodotViewAttribute : Attribute
{
    /// <summary>
    /// 初始化 Godot MVI View 注册特性。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    public MviGodotViewAttribute(string key)
        : this(key, null)
    {
    }

    /// <summary>
    /// 初始化 Godot MVI View 注册特性。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    /// <param name="scenePath">Godot .tscn 场景路径，例如 res://Views/Dashboard/DashboardView.tscn。</param>
    public MviGodotViewAttribute(string key, string? scenePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        ScenePath = scenePath;
    }

    /// <summary>
    /// 获取 View 注册键。
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 获取 Godot .tscn 场景路径。
    /// </summary>
    public string? ScenePath { get; }
}
