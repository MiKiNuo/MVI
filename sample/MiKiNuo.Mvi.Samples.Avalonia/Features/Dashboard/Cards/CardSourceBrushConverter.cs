using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示卡片 SourceKey（"Inpatient"/"Lab"/"Pharmacy"/"Quality"/"Reusable"/"Outpatient" 等）到对应颜色刷的转换器。
/// ConverterParameter 决定取哪一档：Base（主色）/ Soft（浅色）/ Border（边框色）/ Dark（深色强调）。
/// </summary>
public sealed class CardSourceBrushConverter : IValueConverter
{
    /// <summary>单例。</summary>
    public static readonly CardSourceBrushConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        string source = value as string ?? "Other";
        string variant = parameter as string ?? "Base";

        return (source, variant) switch
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

            ("Reusable", "Base") => new SolidColorBrush(Color.Parse("#6366F1")),
            ("Reusable", "Soft") => new SolidColorBrush(Color.Parse("#EEF2FF")),
            ("Reusable", "Border") => new SolidColorBrush(Color.Parse("#C7D2FE")),
            ("Reusable", "Dark") => new SolidColorBrush(Color.Parse("#4338CA")),

            ("Outpatient", "Base") => new SolidColorBrush(Color.Parse("#EC4899")),
            ("Outpatient", "Soft") => new SolidColorBrush(Color.Parse("#FCE7F3")),
            ("Outpatient", "Border") => new SolidColorBrush(Color.Parse("#FBCFE8")),
            ("Outpatient", "Dark") => new SolidColorBrush(Color.Parse("#9D174D")),

            (_, "Base") => new SolidColorBrush(Color.Parse("#64748B")),
            (_, "Soft") => new SolidColorBrush(Color.Parse("#F1F5F9")),
            (_, "Border") => new SolidColorBrush(Color.Parse("#E2E8F0")),
            (_, "Dark") => new SolidColorBrush(Color.Parse("#475569")),
            _ => new SolidColorBrush(Color.Parse("#94A3B8")),
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException("CardSourceBrushConverter 是单向转换器。");
    }
}

/// <summary>
/// 根据 CardState.ActionLog 文本判断当前数据流方向，返回对应颜色的画刷。
/// - 含 "收到父页面或兄弟卡片更新" → 绿色（接收）
/// - 含 "已发起" → 蓝色（派发）
/// - 含 "等待" → 灰色（待协调）
/// - 其他 → 灰色（待命）
/// </summary>
public sealed class ActionLogFlowIndicatorConverter : IValueConverter
{
    /// <summary>单例。</summary>
    public static readonly ActionLogFlowIndicatorConverter Instance = new();

    private static readonly SolidColorBrush BrushReceived = new(Color.Parse("#10B981"));
    private static readonly SolidColorBrush BrushSent = new(Color.Parse("#0EA5E9"));
    private static readonly SolidColorBrush BrushPending = new(Color.Parse("#F59E0B"));
    private static readonly SolidColorBrush BrushIdle = new(Color.Parse("#94A3B8"));

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        string? log = value as string;
        if (string.IsNullOrEmpty(log))
        {
            return BrushIdle;
        }

        if (log.Contains("收到", StringComparison.Ordinal))
        {
            return BrushReceived;
        }

        if (log.Contains("已发起", StringComparison.Ordinal) || log.Contains("提交", StringComparison.Ordinal))
        {
            return BrushSent;
        }

        if (log.Contains("等待", StringComparison.Ordinal))
        {
            return BrushPending;
        }

        return BrushIdle;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException("ActionLogFlowIndicatorConverter 是单向转换器。");
    }
}
