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

    /// <summary>
    /// 转换值。
    /// </summary>
    /// <param name="value">源值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="parameter">转换参数。</param>
    /// <param name="culture">区域信息。</param>
    /// <returns>转换后的值。</returns>
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    /// <summary>
    /// 转换回值。
    /// </summary>
    /// <param name="value">源值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="parameter">转换参数。</param>
    /// <param name="culture">区域信息。</param>
    /// <returns>转换后的值。</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}
