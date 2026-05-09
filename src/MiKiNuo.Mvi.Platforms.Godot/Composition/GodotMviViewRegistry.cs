using global::Godot;

namespace MiKiNuo.Mvi.Platforms.Godot.Composition;

/// <summary>
/// 表示默认 Godot MVI View 注册表。
/// </summary>
public sealed class GodotMviViewRegistry : IGodotMviViewRegistry
{
    private readonly IReadOnlyDictionary<string, Func<Control>> _factories;

    /// <summary>
    /// 初始化 Godot MVI View 注册表。
    /// </summary>
    /// <param name="factories">View 工厂集合。</param>
    public GodotMviViewRegistry(IReadOnlyDictionary<string, Func<Control>> factories)
    {
        _factories = factories ?? throw new ArgumentNullException(nameof(factories));
        Keys = new List<string>(_factories.Keys).AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Keys { get; }

    /// <inheritdoc />
    public Control Create(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (TryCreate(key, out Control? view) && view is not null)
        {
            return view;
        }

        throw new KeyNotFoundException($"找不到 Godot MVI View 注册键：{key}。");
    }

    /// <inheritdoc />
    public bool TryCreate(string key, out Control? view)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_factories.TryGetValue(key, out Func<Control>? factory))
        {
            view = factory();
            return true;
        }

        view = null;
        return false;
    }
}