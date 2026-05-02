using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面意图。
/// </summary>
public abstract partial record BusinessCompositePageIntent : IMviIntent
{
    /// <summary>
    /// 表示刷新页面布局意图。
    /// </summary>
    public sealed partial record RefreshPage : BusinessCompositePageIntent;

    /// <summary>
    /// 表示更新父页面当前上下文意图。
    /// </summary>
    /// <param name="ActiveContext">当前业务上下文。</param>
    /// <param name="FlowStatus">当前流程状态。</param>
    public sealed partial record UpdateContext(string ActiveContext, string FlowStatus) : BusinessCompositePageIntent;

    /// <summary>
    /// 表示追加组合式 MVI 交互日志意图。
    /// </summary>
    /// <param name="Message">交互日志内容。</param>
    public sealed partial record AppendInteractionLog(string Message) : BusinessCompositePageIntent;
}
