using System.Collections.Generic;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot.Composition;

namespace MiKiNuo.Mvi.Samples.Godot.Composition;

/// <summary>
/// 表示 Godot 游戏示例的编译期生成 ViewRegistry。
/// </summary>
[MviGodotGeneratedRegistry]
public sealed partial class GodotSampleGeneratedViewRegistry : IGodotMviViewRegistry
{
    private readonly GodotMviViewRegistry _inner;

    /// <summary>
    /// 初始化 Godot 游戏示例 ViewRegistry。
    /// </summary>
    public GodotSampleGeneratedViewRegistry()
    {
        GodotMviViewRegistryBuilder builder = new();
        RegisterGeneratedViews(builder);
        _inner = builder.Build();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Keys => _inner.Keys;

    /// <inheritdoc />
    public Control Create(string key)
    {
        return _inner.Create(key);
    }

    /// <inheritdoc />
    public bool TryCreate(string key, out Control? view)
    {
        return _inner.TryCreate(key, out view);
    }

    /// <summary>
    /// Auto-generated: 由 MiKiNuo.Mvi.Infrastructure 的 GodotMviViewRegistryGenerator 实现。
    /// </summary>
    /// <param name="builder">Godot View 注册表构建器。</param>
    partial void RegisterGeneratedViews(GodotMviViewRegistryBuilder builder);
}
