using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面状态。
/// </summary>
/// <param name="ScenarioTitle">场景标题。</param>
/// <param name="ScenarioSummary">场景摘要。</param>
/// <param name="PrimaryPanelViewModel">主业务子组件 ViewModel。</param>
/// <param name="SecondaryPanelViewModel">第二业务子组件 ViewModel。</param>
/// <param name="TertiaryPanelViewModel">第三业务子组件 ViewModel。</param>
/// <param name="QuaternaryPanelViewModel">第四业务子组件 ViewModel。</param>
/// <param name="PageLayout">页面布局键。</param>
/// <param name="ActiveContext">当前业务上下文。</param>
/// <param name="FlowStatus">当前流程状态。</param>
/// <param name="InteractionLog">父子 MVI 与子子 MVI 交互日志。</param>
public sealed record BusinessCompositePageState(
    string ScenarioTitle,
    string ScenarioSummary,
    object PrimaryPanelViewModel,
    object SecondaryPanelViewModel,
    object TertiaryPanelViewModel,
    object QuaternaryPanelViewModel,
    string PageLayout,
    string ActiveContext,
    string FlowStatus,
    string InteractionLog) : IMviState;
