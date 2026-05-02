using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.PrescriptionReviewBoard;

/// <summary>
/// 表示处方审核 MVI视图。
/// </summary>
public sealed partial class PrescriptionReviewBoardView : MviAvaloniaView<PrescriptionReviewBoardViewModel>
{
    /// <summary>
    /// 初始化处方审核 MVI视图。
    /// </summary>
    public PrescriptionReviewBoardView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
