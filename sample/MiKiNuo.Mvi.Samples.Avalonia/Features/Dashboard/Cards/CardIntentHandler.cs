using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片意图处理器。
/// 卡片无异步业务,返回空后续意图集合。
/// </summary>
public sealed class CardIntentHandler
    : IMviIntentHandler<CardState, CardIntent, CardEffect>
{
    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>空后续意图集合。</returns>
    public ValueTask<IReadOnlyList<CardIntent>> HandleAsync(
        CardState state,
        CardIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return new ValueTask<IReadOnlyList<CardIntent>>(Array.Empty<CardIntent>());
    }
}
