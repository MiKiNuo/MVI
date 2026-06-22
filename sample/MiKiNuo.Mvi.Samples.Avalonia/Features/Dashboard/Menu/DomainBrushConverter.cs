using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示字符串域键（"Inpatient"/"Lab"/"Pharmacy"/"Quality"/"Other"）到对应颜色刷的转换器。
/// ConverterParameter 决定取哪一档：Base（主色）/ Soft（浅色）/ Border（边框色）/ Dark（深色强调）。
/// </summary>
public sealed class DomainBrushConverter : IValueConverter
{
    /// <summary>单例。</summary>
    public static readonly DomainBrushConverter Instance = new();

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
        string domain = value as string ?? "Other";
        string variant = parameter as string ?? "Base";

        return (domain, variant) switch
        {
            ("Inpatient", "Base") => new SolidColorBrush(Color.Parse("#0EA5E9")),
            ("Inpatient", "Soft") => new SolidColorBrush(Color.Parse("#E0F2FE")),
            ("Inpatient", "Border") => new SolidColorBrush(Color.Parse("#BAE6FD")),
            ("Inpatient", "Dark") => new SolidColorBrush(Color.Parse("#0369A1")),

            ("Lab", "Base") => new SolidColorBrush(Color.Parse("#8B5CF6")),
            ("Lab", "Soft") => new SolidColorBrush(Color.Parse("#EDE9FE")),
            ("Lab", "Border") => new SolidColorBrush(Color.Parse("#DDD6FE")),
            ("Lab", "Dark") => new SolidColorBrush(Color.Parse("#6D28D9")),

            ("Pharmacy", "Base") => new SolidColorBrush(Color.Parse("#F59E0B")),
            ("Pharmacy", "Soft") => new SolidColorBrush(Color.Parse("#FEF3C7")),
            ("Pharmacy", "Border") => new SolidColorBrush(Color.Parse("#FDE68A")),
            ("Pharmacy", "Dark") => new SolidColorBrush(Color.Parse("#B45309")),

            ("Quality", "Base") => new SolidColorBrush(Color.Parse("#10B981")),
            ("Quality", "Soft") => new SolidColorBrush(Color.Parse("#D1FAE5")),
            ("Quality", "Border") => new SolidColorBrush(Color.Parse("#A7F3D0")),
            ("Quality", "Dark") => new SolidColorBrush(Color.Parse("#047857")),

            (_, "Base") => new SolidColorBrush(Color.Parse("#64748B")),
            (_, "Soft") => new SolidColorBrush(Color.Parse("#F1F5F9")),
            (_, "Border") => new SolidColorBrush(Color.Parse("#E2E8F0")),
            (_, "Dark") => new SolidColorBrush(Color.Parse("#475569")),
            _ => new SolidColorBrush(Color.Parse("#94A3B8")),
        };
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
        throw new NotSupportedException("DomainBrushConverter 是单向转换器。");
    }
}
