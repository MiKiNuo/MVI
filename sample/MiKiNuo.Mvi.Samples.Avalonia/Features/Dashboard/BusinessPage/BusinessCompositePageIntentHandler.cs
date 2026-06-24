using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面意图处理器。
/// </summary>
public sealed class BusinessCompositePageIntentHandler
    : IMviIntentHandler<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageMutation, BusinessCompositePageEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<BusinessCompositePageMutation, BusinessCompositePageEffect>> HandleAsync(
        BusinessCompositePageState state,
        BusinessCompositePageIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<BusinessCompositePageMutation, BusinessCompositePageEffect> result = intent switch
        {
            BusinessCompositePageIntent.RefreshPage => MviHandleResult.Empty<BusinessCompositePageMutation, BusinessCompositePageEffect>(),
            BusinessCompositePageIntent.UpdateContext updateContext => HandleUpdateContext(updateContext),
            BusinessCompositePageIntent.AppendInteractionLog appendLog => HandleAppendInteractionLog(state, appendLog),
            _ => MviHandleResult.Empty<BusinessCompositePageMutation, BusinessCompositePageEffect>(),
        };

        return ValueTask.FromResult(result);
    }

    private static MviHandleResult<BusinessCompositePageMutation, BusinessCompositePageEffect> HandleUpdateContext(
        BusinessCompositePageIntent.UpdateContext intent)
    {
        return MviHandleResult.Mutations<BusinessCompositePageMutation, BusinessCompositePageEffect>(
            new BusinessCompositePageMutation.SetActiveContext(intent.ActiveContext),
            new BusinessCompositePageMutation.SetFlowStatus(intent.FlowStatus));
    }

    private static MviHandleResult<BusinessCompositePageMutation, BusinessCompositePageEffect> HandleAppendInteractionLog(
        BusinessCompositePageState state,
        BusinessCompositePageIntent.AppendInteractionLog intent)
    {
        string nextLog = string.IsNullOrWhiteSpace(state.InteractionLog)
            ? intent.Message
            : $"{intent.Message}\n{state.InteractionLog}";

        return MviHandleResult.Mutations<BusinessCompositePageMutation, BusinessCompositePageEffect>(
            new BusinessCompositePageMutation.SetInteractionLog(nextLog));
    }
}
