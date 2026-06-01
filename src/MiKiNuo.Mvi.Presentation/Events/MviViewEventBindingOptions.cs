namespace MiKiNuo.Mvi.Presentation.Events;

/// <summary>
/// 表示 ViewEvent 命令绑定选项。
/// </summary>
/// <param name="Debounce">防抖时间。</param>
public sealed record MviViewEventBindingOptions(TimeSpan? Debounce = null)
{
    /// <summary>
    /// 获取空绑定选项。
    /// </summary>
    public static MviViewEventBindingOptions None { get; } = new();
}
