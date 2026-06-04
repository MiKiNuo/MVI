using Avalonia.Data.Converters;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 bool 取反的转换器。用于 ListBoxItem 模板中"未选中"分支的可见性绑定。
/// Avalonia 不支持 <c>{TemplateBinding !Property}</c> 语法，因此用此转换器模拟。
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    /// <summary>单例。</summary>
    public static readonly InverseBooleanConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}
