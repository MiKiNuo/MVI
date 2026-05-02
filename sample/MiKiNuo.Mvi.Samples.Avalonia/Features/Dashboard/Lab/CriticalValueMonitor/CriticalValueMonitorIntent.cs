using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.CriticalValueMonitor;

/// <summary>
/// 表示危急值闭环 MVI意图。
/// </summary>
public abstract partial record CriticalValueMonitorIntent : IMviIntent
{
    /// <summary>
    /// 表示执行主业务动作意图。
    /// </summary>
    public sealed partial record ExecutePrimaryAction : CriticalValueMonitorIntent;

    /// <summary>
    /// 表示执行辅助业务动作意图。
    /// </summary>
    public sealed partial record ExecuteSecondaryAction : CriticalValueMonitorIntent;

    /// <summary>
    /// 表示应用来自父页面或兄弟 MVI 的外部更新意图。
    /// </summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : CriticalValueMonitorIntent;
}
