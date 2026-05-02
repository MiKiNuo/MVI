namespace MiKiNuo.Mvi.Abstractions.Binding;

/// <summary>
/// 表示双向绑定属性的更新模式。
/// </summary>
public enum MviTwoWayUpdateMode
{
    /// <summary>
    /// Setter 只派发 Intent，属性值由 State 回流更新。
    /// </summary>
    StateFirst = 0,

    /// <summary>
    /// Setter 先更新本地属性，再派发 Intent。
    /// </summary>
    Optimistic = 1,
}
