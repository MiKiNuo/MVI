using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面状态。
/// </summary>
/// <param name="ScenarioTitle">场景标题。</param>
/// <param name="ScenarioSummary">场景摘要。</param>
/// <param name="PageLayout">页面布局键（决定 4 个数据流节点分别承载哪 4 张卡片）。</param>
/// <param name="ActiveContext">当前业务上下文。</param>
/// <param name="FlowStatus">当前流程状态。</param>
/// <param name="InteractionLog">父子 MVI 与子子 MVI 交互日志。</param>
public sealed record BusinessCompositePageState(
    string ScenarioTitle,
    string ScenarioSummary,
    string PageLayout,
    string ActiveContext,
    string FlowStatus,
    string InteractionLog) : IMviState;
