using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面意图。
/// </summary>
public abstract partial record OutpatientWorkstationIntent : IMviIntent
{
    /// <summary>
    /// 表示刷新页面布局意图。
    /// </summary>
    public sealed partial record RefreshPage : OutpatientWorkstationIntent;
}
