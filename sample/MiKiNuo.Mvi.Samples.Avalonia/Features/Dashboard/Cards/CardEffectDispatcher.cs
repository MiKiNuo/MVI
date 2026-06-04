using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一副作用分发器。
/// 接收 CardEffect 后：
/// 1. 根据 source PageKey 在 <see cref="DashboardCardRegistry"/> 中查找同 SourceKey 组的兄弟卡片，
///    向每个兄弟 store 同步派发 <see cref="CardIntent.ApplyExternalUpdate"/>，实现组合页面内跨卡片数据流；
/// 2. 随后向 <see cref="IMviMediator"/> 发送 <see cref="DashboardComponentInteractionRequest"/>，
///    由容器侧 Mediator 路由把消息追加到父级 BusinessCompositePage 的 InteractionLog。
/// </summary>
public sealed class CardEffectDispatcher : IMviEffectDispatcher<CardEffect>
{
    private const string PrimaryActionKey = "主业务动作";
    private const string SecondaryActionKey = "辅助业务动作";
    private const string FormSubmitActionKey = "提交表单";

    private readonly IMviMediator _mediator;
    private readonly PageKey _sourceKey;
    private readonly IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> _allStores;

    /// <summary>
    /// 初始化仪表板卡片副作用分发器。
    /// </summary>
    /// <param name="mediator">Mediator。</param>
    /// <param name="sourceKey">本卡片对应的 PageKey，用于定位同组兄弟。</param>
    /// <param name="allStores">所有 16 张卡片的 store 字典（按 PageKey 索引）。本分发器会从中按 GetSiblingKeys 过滤。</param>
    public CardEffectDispatcher(
        IMviMediator mediator,
        PageKey sourceKey,
        IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> allStores)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(allStores);

        _mediator = mediator;
        _sourceKey = sourceKey;
        _allStores = allStores;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(CardEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is CardEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await NotifySiblingsAsync(primaryWorkflow.ContextText, cancellationToken).ConfigureAwait(false);
            await SendAsync(primaryWorkflow.ContextText, PrimaryActionKey, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is CardEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await NotifySiblingsAsync(secondaryWorkflow.ContextText, cancellationToken).ConfigureAwait(false);
            await SendAsync(secondaryWorkflow.ContextText, SecondaryActionKey, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is CardEffect.RequestFormSubmission formSubmission)
        {
            await NotifySiblingsAsync(formSubmission.ContextText, cancellationToken).ConfigureAwait(false);
            await SendAsync(formSubmission.ContextText, FormSubmitActionKey, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 向同 SourceKey 组内的兄弟卡片 store 派发 <see cref="CardIntent.ApplyExternalUpdate"/>。
    /// 跳过自身（避免回环）；不同 SourceKey 的卡片不在派发范围内，由 <see cref="DashboardCardRegistry.GetSiblingKeys"/> 隔离。
    /// </summary>
    /// <param name="contextText">从源卡片派发的上下文文本。</param>
    /// <param name="cancellationToken">取消标记。</param>
    private async ValueTask NotifySiblingsAsync(string contextText, CancellationToken cancellationToken)
    {
        IReadOnlyList<PageKey> siblings = DashboardCardRegistry.GetSiblingKeys(_sourceKey);
        if (siblings.Count == 0)
        {
            return;
        }

        CardIntent intent = new CardIntent.ApplyExternalUpdate(contextText);
        foreach (PageKey siblingKey in siblings)
        {
            if (!_allStores.TryGetValue(siblingKey, out IMviStore<CardState, CardIntent, CardEffect>? store))
            {
                continue;
            }

            await store.DispatchAsync(intent, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask SendAsync(string contextText, string actionKey, CancellationToken cancellationToken)
    {
        // 实际 PageKey/SourceKey 通过全局上下文已知（来自当前 store），Mediator.SendComponentInteractionAsync
        // 目前沿用现有 string 路由；后续 Mediator 升级为强类型 request/response 后替换为 PageKey 路由（详见 .gsd/DECISIONS.md 2026-06-02 第 2 轮）。
        await _mediator.SendComponentInteractionAsync(
            "Dashboard",
            "Card",
            actionKey,
            contextText,
            cancellationToken).ConfigureAwait(false);
    }
}
