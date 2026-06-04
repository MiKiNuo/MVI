using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示菜单项 IconKey 字符串到 <see cref="StreamGeometry"/> 的转换器。
/// 支持 6 个图标：Outpatient/Inpatient/Lab/Pharmacy/Quality/Arch，匹配 <see cref="DashboardMenuItemDescriptor.IconKey"/>。
/// </summary>
public sealed class IconKeyToGeometryConverter : IValueConverter
{
    /// <summary>单例。</summary>
    public static readonly IconKeyToGeometryConverter Instance = new();

    private static readonly StreamGeometry IconOutpatient = StreamGeometry.Parse("M3 7l9-4 9 4-9 4-9-4zm0 5l9 4 9-4M3 17l9 4 9-4");
    private static readonly StreamGeometry IconInpatient = StreamGeometry.Parse("M12 2L2 7v6c0 5 4 9 10 11 6-2 10-6 10-11V7l-10-5zm0 6h2v4h4v2h-4v4h-2v-4H8v-2h4V8z");
    private static readonly StreamGeometry IconLab = StreamGeometry.Parse("M9 2v6L4 18a3 3 0 003 3h10a3 3 0 003-3l-5-10V2H9zm2 2h2v5l4 8H7l4-8V4z");
    private static readonly StreamGeometry IconPharmacy = StreamGeometry.Parse("M3 3h18l-2 13H5L3 3zm2 16h14v2H5v-2zm2-12h10v2H7V7zm2 4h6v2H9v-2z");
    private static readonly StreamGeometry IconQuality = StreamGeometry.Parse("M12 1L3 5v6c0 5.5 3.8 10.7 9 12 5.2-1.3 9-6.5 9-12V5l-9-4zm-1 14l-4-4 1.4-1.4L11 12.2l5.6-5.6L18 8l-7 7z");
    private static readonly StreamGeometry IconArch = StreamGeometry.Parse("M12 2L1 9l11 7 11-7L12 2zm0 4.5L18.5 9 12 12.5 5.5 9 12 6.5zM3 13.5l9 5.5 9-5.5v2.5l-9 5.5-9-5.5v-2.5z");

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        string iconKey = value as string ?? string.Empty;
        return iconKey switch
        {
            "icon-outpatient" => IconOutpatient,
            "icon-inpatient" => IconInpatient,
            "icon-lab" => IconLab,
            "icon-pharmacy" => IconPharmacy,
            "icon-quality" => IconQuality,
            "icon-arch" => IconArch,
            _ => IconArch,
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException("IconKeyToGeometryConverter 是单向转换器。");
    }
}
