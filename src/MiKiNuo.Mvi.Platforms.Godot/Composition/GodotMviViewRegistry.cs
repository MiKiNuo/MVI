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
        ArgumentNullException.ThrowIfNull(factories);
        _factories = factories;
        Keys = new List<string>(_factories.Keys).AsReadOnly();
    }

    /// <summary>
    /// 获取已经注册的 View 键集合。
    /// </summary>
    public IReadOnlyCollection<string> Keys { get; }

    /// <summary>
    /// 根据键创建 View 实例。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    /// <returns>创建出的 Control 实例。</returns>
    public Control Create(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (TryCreate(key, out Control? view) && view is not null)
        {
            return view;
        }

        throw new KeyNotFoundException($"找不到 Godot MVI View 注册键：{key}。");
    }

    /// <summary>
    /// 尝试根据键创建 View 实例。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    /// <param name="view">创建出的 View。</param>
    /// <returns>如果创建成功则返回 true。</returns>
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