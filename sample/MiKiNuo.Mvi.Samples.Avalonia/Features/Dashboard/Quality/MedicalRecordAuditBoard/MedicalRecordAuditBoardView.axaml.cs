using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.MedicalRecordAuditBoard;

/// <summary>
/// 表示病历质控 MVI视图。
/// </summary>
public sealed partial class MedicalRecordAuditBoardView : MviAvaloniaView<MedicalRecordAuditBoardViewModel>
{
    /// <summary>
    /// 初始化病历质控 MVI视图。
    /// </summary>
    public MedicalRecordAuditBoardView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
