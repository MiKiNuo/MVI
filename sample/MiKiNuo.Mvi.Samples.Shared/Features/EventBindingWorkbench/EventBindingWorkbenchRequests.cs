namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合示例内的组件交互请求。
/// </summary>
/// <param name="SourceComponent">来源组件。</param>
/// <param name="ActionKey">动作键。</param>
/// <param name="ContextText">上下文文本。</param>
public sealed record EventBindingWorkbenchInteractionRequest(
    string SourceComponent,
    string ActionKey,
    string ContextText);

/// <summary>
/// 表示事件绑定组合示例内的组件交互响应。
/// </summary>
/// <param name="Message">响应消息。</param>
/// <param name="Changed">是否产生变化。</param>
public sealed record EventBindingWorkbenchInteractionResponse(string Message, bool Changed);
