using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedOverview;

/// <summary>
/// 表示床位总览 MVI意图。
/// </summary>
public abstract partial record BedOverviewIntent : IMviIntent
{
    /// <summary>
    /// 表示执行主业务动作意图。
    /// </summary>
    public sealed partial record ExecutePrimaryAction : BedOverviewIntent;

    /// <summary>
    /// 表示执行辅助业务动作意图。
    /// </summary>
    public sealed partial record ExecuteSecondaryAction : BedOverviewIntent;

    /// <summary>
    /// 表示应用来自父页面或兄弟 MVI 的外部更新意图。
    /// </summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : BedOverviewIntent;
}
