namespace MiKiNuo.Mvi.Presentation.Events;

/// <summary>
/// 表示跨平台输入修饰键。
/// </summary>
[Flags]
public enum MviInputModifiers
{
    /// <summary>
    /// 无修饰键。
    /// </summary>
    None = 0,

    /// <summary>
    /// Shift 修饰键。
    /// </summary>
    Shift = 1,

    /// <summary>
    /// Control 修饰键。
    /// </summary>
    Control = 2,

    /// <summary>
    /// Alt 修饰键。
    /// </summary>
    Alt = 4,

    /// <summary>
    /// Meta 或 Command 修饰键。
    /// </summary>
    Meta = 8
}
