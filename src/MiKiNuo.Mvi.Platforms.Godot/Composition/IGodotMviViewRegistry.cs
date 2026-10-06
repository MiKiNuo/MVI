using global::Godot;

namespace MiKiNuo.Mvi.Platforms.Godot.Composition;

/// <summary>
/// 表示 Godot MVI View 的运行时创建注册表。
/// </summary>
public interface IGodotMviViewRegistry
{
    /// <summary>
    /// 获取已经注册的 View 键集合。
    /// </summary>
    public IReadOnlyCollection<string> Keys { get; }

    /// <summary>
    /// 根据键创建 View 实例。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    /// <returns>创建出的 Control 实例。</returns>
    public Control Create(string key);

    /// <summary>
    /// 尝试根据键创建 View 实例。
    /// </summary>
    /// <param name="key">View 注册键。</param>
    /// <param name="view">创建出的 View。</param>
    /// <returns>如果创建成功则返回 true。</returns>
    public bool TryCreate(string key, out Control? view);
}