using Avalonia;
using Avalonia.Controls;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Slot;

/// <summary>
/// 表示 Avalonia MVI 组合插槽。
/// </summary>
public sealed class MviSlotHost : ContentControl
{
    /// <summary>
    /// 定义组件编号属性。
    /// </summary>
    public static readonly StyledProperty<string?> ComponentIdProperty =
        AvaloniaProperty.Register<MviSlotHost, string?>(nameof(ComponentId));

    /// <summary>
    /// 获取或设置组件编号。
    /// </summary>
    public string? ComponentId
    {
        get => GetValue(ComponentIdProperty);
        set => SetValue(ComponentIdProperty, value);
    }
}
